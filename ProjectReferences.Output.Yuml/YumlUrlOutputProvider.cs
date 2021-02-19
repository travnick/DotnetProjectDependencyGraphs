using System.IO;

using ProjectReferences.Interfaces;
using ProjectReferences.Models;
using ProjectReferences.Shared;

namespace ProjectReferences.Output.Yuml
{
    public class YumlUrlOutputProvider : IOutputProvider
    {
        public OutputResponse Create(RootNode rootNode, string outputFolder)
        {
            Logger.Log("Creating instance of YumlUrlOutputProvider", LogLevel.High);

            var translator = new RootNodeToYumlClassDiagramTranslator(rootNode.ChildProjects);
            var yumlClassOutput = translator.Translate(rootNode, true);

            string outputTree = YumlHelper.CommaSeperateRelationshipsOnMultipleLines(YumlHelper.ReplaceSpaces(yumlClassOutput.DependencyDiagram.ToString()));
            string filePath = Path.Combine(Path.GetFullPath(outputFolder), Path.Combine(outputFolder, rootNode.File.Name + ".url.yuml"));

            FileHandler.WriteToOutputFile(filePath, YumlHelper.YumlClassUrl + outputTree);

            return new OutputResponse { Success = true, Path = filePath };
        }
    }
}