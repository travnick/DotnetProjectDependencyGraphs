using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectReferences.Interfaces;
using ProjectReferences.Models;
using ProjectReferences.Output;
using ProjectReferences.Shared;

namespace ProjectReference
{
    public static class Manager
    {
        /// <summary>
        /// Creates the rootNode collection from the analysisRequest.  Will interigate the solution and proejcts to find all other projects and their relationships.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static RootNode CreateRootNode(AnalysisRequest request)
        {
            if (!File.Exists(request.RootFile))
            {
                throw new FileNotFoundException(request.RootFile);
            }

            var rootFileInfo = new FileInfo(request.RootFile);

            var rootNode = new RootNode
            {
                Directory = rootFileInfo.Directory,
                File = rootFileInfo,
                Name = rootFileInfo.Name,
                NodeType = DetermineRootNodeType(rootFileInfo.FullName),
                SearchDepth = request.NumberOfLevelsToDig
            };

            return rootNode;
        }

        private static RootNodeType DetermineRootNodeType(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentOutOfRangeException();
            }

            if (filePath.ToLower().Trim().EndsWith(".sln"))
            {
                return RootNodeType.SLN;
            }

            if (filePath.ToLower().Trim().EndsWith(".csproj"))
            {
                return RootNodeType.CSPROJ;
            }

            throw new ArgumentOutOfRangeException("unknown file extension");
        }

        public static void Process(RootNode rootNode, bool includeExternalReferences)
        {
            switch (rootNode.NodeType)
            {
                case RootNodeType.SLN:
                    ProcessSlnRootNode(rootNode, includeExternalReferences);
                    return;
                case RootNodeType.CSPROJ:
                    ProcessCsProjRootNode(rootNode, includeExternalReferences);
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void ProcessSlnRootNode(RootNode rootNode, bool includeExternalReferences)
        {
            var projectLinks = SolutionFileManager.FindAllProjectLinks(rootNode);
            ProcessLinks(projectLinks, rootNode, includeExternalReferences);
        }

        private static void ProcessCsProjRootNode(RootNode rootNode, bool includeExternalReferences)
        {
            ProcessLinks(new HashSet<InvestigationLink> { new InvestigationLink { FullPath = rootNode.File.FullName, Parent = null } }, rootNode, includeExternalReferences);
        }

        private static void ProcessLinks(ISet<InvestigationLink> linksToBeInvestigated, RootNode rootNode, bool includeExternalReferences)
        {
            while (linksToBeInvestigated.Any())
            {
                var investigation = linksToBeInvestigated.First();
                linksToBeInvestigated.Remove(investigation);

                var projectDetail = ProjectFactory.MakeProjectDetail(investigation.FullPath, includeExternalReferences);
                if (investigation.Parent != null)
                {
                    projectDetail.ParentProjects.Add(new ProjectLinkObject(rootNode.ChildProjects.GetById(investigation.Parent.Id)));
                }

                var link = new ProjectLinkObject(projectDetail);

                linksToBeInvestigated.UnionWith(projectDetail.ChildProjects.Select(p => new InvestigationLink { FullPath = p.FullPath, Parent = link }));

                rootNode.ChildProjects.Add(projectDetail);
            }
        }

        public static OutputResponse CreateOutput(AnalysisRequest request, RootNode rootNode)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (rootNode == null) throw new ArgumentNullException("rootNode");

            IOutputProvider outputProvider = OutputFactory.CreateProvider(request.OutputType);
            return outputProvider.Create(rootNode, Path.Combine(Directory.GetCurrentDirectory(), request.OutputFolder));
        }
    }
}
