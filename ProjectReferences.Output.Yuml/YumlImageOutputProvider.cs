using System.IO;
using ProjectReferences.Interfaces;
using ProjectReferences.Models;
using ProjectReferences.Shared;

namespace ProjectReferences.Output.Yuml
{
    public class YumlImageOutputProvider : IOutputProvider
    {
        public OutputResponse Create(RootNode rootNode, string outputFolder)
        {
            Logger.Log("Creating instance of YumlImageOutputProvider", LogLevel.High);

            string serverImagePath = GenerateImageOnServer(rootNode);

            string outputFileName = DownloadImage(rootNode, outputFolder, serverImagePath);

            return new OutputResponse { Success = true, Path = outputFileName };
        }

        private static string GenerateImageOnServer(RootNode rootNode)
        {
            var translator = new RootNodeToYumlClassDiagramTranslator(rootNode.ChildProjects);
            var yumlClassOutput = translator.Translate(rootNode, true);

            var serverImagePath = YumlHelper.GenerateImageOnYumlServer(yumlClassOutput.DependencyDiagram);
            return serverImagePath;
        }

        private static string DownloadImage(RootNode rootNode, string outputFolder, string serverImagePath)
        {
            string basePath = Path.GetFullPath(outputFolder);
            var outputFileName = Path.Combine(basePath, rootNode.File.Name + ".svg");
            YumlHelper.DownloadYumlServerImage(outputFileName, serverImagePath);
            return outputFileName;
        }
    }
}