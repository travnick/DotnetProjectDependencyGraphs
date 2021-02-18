using System;
using System.IO;
using System.Linq;
using System.Text;
using ProjectReferences.Interfaces;
using ProjectReferences.Models;
using ProjectReferences.Output.Yuml;
using ProjectReferences.Output.Yuml.Models;
using ProjectReferences.Shared;

namespace ProjectReferences.Output.Html
{
    public sealed class SinglePageHtmlDocumentOutputProvider : IOutputProvider
    {
        public OutputResponse Create(RootNode rootNode, string outputFolder)
        {
            Logger.Log("Creating instance of SinglePageHtmlDocumentOutputProvider", LogLevel.High);

            var builder = new StringBuilder();

            _ = builder.AppendLine(@"<html>");

            AppendHtmlHead(builder);

            _ = builder.AppendLine(@"<body>");

            var translator = new RootNodeToYumlClassDiagramTranslator(rootNode.ChildProjects);

            AddOverallRootDependencies(rootNode, outputFolder, builder, translator);

            _ = builder.AppendLine(@"<div id='accordian'>");

            AddReferences(rootNode, outputFolder, builder, translator);

            _ = builder.AppendLine(@"</div>");

            _ = builder.AppendLine(@"</body>");
            _ = builder.AppendLine(@"</html>");

            string htmlOutputFilePath = Path.Combine(outputFolder, "references.html");

            FileHandler.EnsureFolderExistsForFullyPathedLink(htmlOutputFilePath);
            File.WriteAllText(htmlOutputFilePath, builder.ToString());

            return new OutputResponse { Path = htmlOutputFilePath, Success = true };
        }

        private static void AddOverallRootDependencies(RootNode rootNode, string outputFolder, StringBuilder builder, RootNodeToYumlClassDiagramTranslator translator)
        {
            var yumlClassOutput = translator.Translate(rootNode, true);

            _ = builder.AppendLine(string.Format(@"<h1>All references for: {0}</h1>", yumlClassOutput.RootFile));

            string rootNodeOutputFileName = MakeOutputImageFileName(outputFolder, yumlClassOutput.RootFile);

            FetchImage(yumlClassOutput.DependencyDiagram, rootNodeOutputFileName);

            _ = builder.AppendLine(string.Format(@"<p>Image for whole reference list: <a href='{0}' target='_blank'> View Yuml Image</a></p>", rootNodeOutputFileName));
        }

        private static void AddReferences(RootNode rootNode, string outputFolder, StringBuilder builder, RootNodeToYumlClassDiagramTranslator translator)
        {
            foreach (var projectDetail in rootNode.ChildProjects.OrderBy(x => Path.GetFileName(x.FullPath)))
            {
                Logger.Log(string.Format("generating HTML output for projectDetail: '{0}'", projectDetail.FullPath), LogLevel.High);

                var projectOutput = translator.Translate(projectDetail, true);

                _ = builder.AppendLine(string.Format(@"<h2>{0}</h2>", Path.GetFileName(projectOutput.RootFile)));
                _ = builder.AppendLine(string.Format(@"<div class='projectReference' id='{0}'>", projectDetail.Id));

                AddReferences(builder, outputFolder, projectDetail, projectOutput);

                AddReferencedBy(builder, outputFolder, projectDetail, projectOutput);

                _ = builder.AppendLine(@"</div>");
            }
        }

        private static void AddReferences(StringBuilder builder, string outputFolder, ProjectDetail projectDetail, YumlClassOutput projectOutput)
        {
            if (projectOutput.DependencyDiagram.Relationships.Count > 0)
            {
                string projectOutputFileName = MakeOutputImageFileName(outputFolder, projectOutput.RootFile);
                FetchImage(projectOutput.DependencyDiagram, projectOutputFileName);
                _ = builder.AppendLine(string.Format(@"<h2>{0} - <a href='{1}' target='_blank'>View Yuml Image</a></h2>", Path.GetFileName(projectOutput.RootFile), projectOutputFileName));
            }

            if (projectDetail.ChildProjects.Any())
            {
                _ = builder.AppendLine(@"<p>This project references:</p>");
                _ = builder.AppendLine(@"<ul>");
                foreach (var reference in projectDetail.ChildProjects)
                {
                    _ = builder.AppendLine(string.Format(@"<li><a href='#{0}'>{1}</a></li>", reference.Id, Path.GetFileName(reference.FullPath)));
                }
                _ = builder.AppendLine(@"</ul>");
            }
            else
            {
                _ = builder.AppendLine("<p>This project does not reference any other projects</p>");
            }
        }

        private static void AddReferencedBy(StringBuilder builder, string outputFolder, ProjectDetail projectDetail, YumlClassOutput projectOutput)
        {
            if (projectDetail.ParentProjects.Any())
            {
                _ = builder.AppendLine(@"<p>This project is referenced by:</p>");

                if (projectOutput.ParentDiagram.Relationships.Count > 0)
                {
                    string projectOutputFileName = MakeParentOutputImageFileName(outputFolder, projectOutput.RootFile);
                    FetchImage(projectOutput.ParentDiagram, projectOutputFileName);
                    _ = builder.AppendLine(string.Format(@"<a href='{1}' target='_blank'>View Yuml Image</a>", Path.GetFileName(projectOutput.RootFile), projectOutputFileName));
                }

                _ = builder.AppendLine(@"<ul>");

                foreach (var reference in projectDetail.ParentProjects.OrderBy(x => Path.GetFileName(x.FullPath)))
                {
                    _ = builder.AppendLine(string.Format(@"<li><a href='#{0}'>{1}</a></li>", reference.Id, Path.GetFileName(reference.FullPath)));
                }

                _ = builder.AppendLine(@"</ul>");
            }
            else
            {
                _ = builder.AppendLine("<p>This project is not referenced by any other projects</p>");
            }
        }

        private static void AppendHtmlHead(StringBuilder builder)
        {
            _ = builder.AppendLine(@"<head>");
            _ = builder.AppendLine(@"<script src='http://ajax.googleapis.com/ajax/libs/jquery/1.9.1/jquery.min.js'></script>");
            _ = builder.AppendLine(@"<script src='http://ajax.googleapis.com/ajax/libs/jqueryui/1.10.2/jquery-ui.min.js'></script>");
            _ = builder.AppendLine(@"<link rel='stylesheet' type='text/css' href='http://ajax.googleapis.com/ajax/libs/jqueryui/1.10.2/themes/eggplant/jquery-ui.css' />");

            _ = builder.AppendLine(@"
                <script>
                    $(document).ready(function() {
                        $('#accordian').accordion();

                        $('li').click(function() {
                            $('#accordian').accordion( 'option', 'active', $('#accordian .projectReference').index($($(this).find('a').attr('href'))));
                            return false;
                        });

                    });
                </script>"
            );

            _ = builder.AppendLine(@"</head>");
        }

        private static void FetchImage(YumlOutput.Class.YumlClassDiagram projectOutput, string projectOutputFileName)
        {
            if (!File.Exists(projectOutputFileName))
            {
                YumlHelper.DownloadYumlServerImage(projectOutputFileName, YumlHelper.GenerateImageOnYumlServer(projectOutput));
            }
        }

        private static string MakeOutputImageFileName(string outputFolder, string rootFile)
        {
            return Path.Combine(outputFolder, Path.GetFileName(rootFile)) + ".svg";
        }
        private static string MakeParentOutputImageFileName(string outputFolder, string rootFile)
        {
            return Path.Combine(outputFolder, Path.GetFileName(rootFile)) + "_parents.svg";
        }
    }
}
