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
    public class Manager
    {
        /// <summary>
        /// Creates the rootNode collection from the analysisRequest.  Will interigate the solution and proejcts to find all other projects and their relationships.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static RootNode CreateRootNode(string rootFile, AnalysisRequest request)
        {
            if (!File.Exists(rootFile))
            {
                throw new FileNotFoundException(rootFile);
            }

            var rootFileInfo = new FileInfo(rootFile);

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

        public void Process(RootNode rootNode, bool includeExternalReferences)
        {
            switch (rootNode.NodeType)
            {
                case RootNodeType.SLN:
                    ProcessSlnRootNode(rootNode, includeExternalReferences);
                    break;

                case RootNodeType.CSPROJ:
                    ProcessCsProjRootNode(rootNode.ChildProjects, rootNode.File.FullName, includeExternalReferences);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(string.Format("Node type is not supported '{0}'", rootNode.NodeType));
            }

            rootNode.OptimizeReferences();
        }

        public static OutputResponse CreateOutput(AnalysisRequest request, RootNode rootNode)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            if (rootNode == null)
            {
                throw new ArgumentNullException("rootNode");
            }

            var outputProvider = OutputFactory.CreateProvider(request.OutputType);
            return outputProvider.Create(rootNode, Path.Combine(Directory.GetCurrentDirectory(), request.OutputFolder));
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

        private void ProcessSlnRootNode(RootNode rootNode, bool includeExternalReferences)
        {
            var projectLinks = SolutionFileManager.FindAllProjectLinks(rootNode);
            ProcessLinks(rootNode.ChildProjects, projectLinks, includeExternalReferences);
        }

        private void ProcessCsProjRootNode(ProjectDetailRepository projectRepository, string fullPath, bool includeExternalReferences)
        {
            ProcessLinks(projectRepository, new HashSet<InvestigationLink> { new InvestigationLink(null, ProjectLinkObject.MakeOutOfSolutionLink(fullPath)) }, includeExternalReferences);
        }

        private void ProcessLinks(ProjectDetailRepository projectRepository, ISet<InvestigationLink> linksToBeInvestigated, bool includeExternalReferences)
        {
            while (linksToBeInvestigated.Any())
            {
                var investigation = linksToBeInvestigated.First();
                _ = linksToBeInvestigated.Remove(investigation);

                if (!investigation.IsProjectLoadable)
                {
                    continue;
                }

                if (!projectRepository.Has(investigation.Guid))
                {
                    var projectDetail = _projectFactory.MakeProjectDetail(projectRepository, investigation.FullPath, investigation.Guid, includeExternalReferences);
                    if (investigation.Parent != null)
                    {
                        _ = projectDetail.ParentProjects.Add(new ProjectLinkObject(projectRepository.GetById(investigation.Parent.Id)));
                    }

                    var parent = new ProjectLinkObject(projectDetail);

                    var children = projectDetail.ChildProjects.Select(child => new InvestigationLink(parent, child));

                    linksToBeInvestigated.UnionWith(children);

                    projectRepository.Add(projectDetail);
                }
            }
        }

        private readonly ProjectFactory _projectFactory = new ProjectFactory();
    }
}
