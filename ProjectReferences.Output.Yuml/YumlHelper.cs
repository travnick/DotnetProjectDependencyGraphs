using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using ProjectReferences.Shared;
using YumlOutput.Class;

namespace ProjectReferences.Output.Yuml
{
    public sealed class YumlHelper
    {
        public static string EncodeForHttpPost(string message)
        {
            string value = message.Trim();

            value = Regex.Replace(value, @"^\s$[\r\n]*", "", RegexOptions.Multiline);
            value = value.Replace("+", "%2B");
            value = value.Replace("[", "%5B");
            value = value.Replace("]", "%5D");
            value = value.Replace(">", "%3E");
            value = value.Replace(" ", "+");
            value = value.Replace(":", "%3A");
            value = value.Replace(";", "%3B");
            value = value.Replace("|", "%7C");
            value = value.Replace("\r\n", "%2C+");

            return value;
        }

        public static string YumlClassUrl { get { return @"https://yuml.me/diagram/nofunky/class"; } }

        public static string YumlImageUrl { get { return @"https://yuml.me"; } }

        public static string ReplaceSpaces(string url)
        {
            return url.Replace(" ", "%20");
        }

        public static string CommaSeperateRelationshipsOnMultipleLines(string relationshsipTree)
        {
            string tree = relationshsipTree.Replace(Environment.NewLine, ",");
            if (tree.EndsWith(","))
            {
                tree = tree.Substring(0, tree.Length - 1);
            }
            return tree;
        }

        /// <summary>
        /// Generates a document on the server and returns the full url to it.
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public static string GenerateImageOnYumlServer(YumlClassDiagram output)
        {
            Logger.Log(string.Format("Generating image on yuml server, class diagram: '{0}'", output), LogLevel.High);

            ServicePointManager.Expect100Continue = false;
            var req = WebRequest.Create(YumlClassUrl);

            //req.Proxy = new System.Net.WebProxy(ProxyString, true);
            //Add these, as we're doing a POST
            req.ContentType = "application/x-www-form-urlencoded";
            req.Method = "POST";
            //We need to count how many bytes we're sending. Post'ed Faked Forms should be name=value&
            string diagramDescriptionRaw = output.ToString();
            string diagramDescription = "dsl_text=" + EncodeForHttpPost(diagramDescriptionRaw);
            byte[] bytes = Encoding.ASCII.GetBytes(diagramDescription);
            req.ContentLength = bytes.Length;

            using (var outputStream = req.GetRequestStream())
            {
                outputStream.Write(bytes, 0, bytes.Length); //Push it out there
                outputStream.Close();
            }

            string htmlContent;

            using (var response = req.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    htmlContent = reader.ReadToEnd().Trim();
                }
            }

            var imageFetcher = new ImageFetcher(htmlContent);

            var thread = new Thread(imageFetcher.FetchImageSrc);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return imageFetcher.ImageUrl;
        }

        private sealed class ImageFetcher
        {
            public ImageFetcher(string htmlContent)
            {
                this.htmlContent = htmlContent;
            }

            public void FetchImageSrc()
            {
                using (var browser = new WebBrowser())
                {
                    string src = htmlContent;

                    using (var client = new WebClient())
                    {
                        ImageUrl = YumlImageUrl + "/" + src;

                        Logger.Log(string.Format("image has been generated on server with url of :'{0}'", ImageUrl), LogLevel.High);
                    }
                }
            }

            private readonly string htmlContent;

            public string ImageUrl { get; private set; }
        }

        public static void DownloadYumlServerImage(string outputFileName, string url)
        {
            Logger.Log(string.Format("Downloading image from '{0}' saving to '{1}'", url, outputFileName), LogLevel.High);
            FileHandler.EnsureFolderExistsForFullyPathedLink(outputFileName);

            var client = new WebClient();
            client.DownloadFile(url, outputFileName);
        }
    }
}
