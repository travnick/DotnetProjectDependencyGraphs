using System.Collections.Generic;

using ProjectReferences.Models;
using ProjectReferences.Output.Yuml.Models;
using ProjectReferences.Shared;

using YumlOutput.Class;
using YumlOutput.Class.Models;
using YumlOutput.Class.Relationships;

using static ProjectReferences.Models.ProjectDetail;

namespace ProjectReferences.Output.Yuml
{
    public class RootNodeToYumlClassDiagramTranslator
    {
        public RootNodeToYumlClassDiagramTranslator(ProjectDetailRepository projectRepository)
        {
            ProjectRepository = projectRepository;
        }

        public YumlClassOutput Translate(RootNode rootNode, bool newlineForEachRelationship = false)
        {
            Logger.Log("Translating rootNode to YumlClassOutput", LogLevel.High);

            return new YumlClassOutput(MakeDependenciesDiagram(rootNode, newlineForEachRelationship), new YumlClassDiagram(), rootNode.File.FullName);
        }

        public YumlClassOutput Translate(ProjectDetail projectDetail, bool newlineForEachRelationship = false)
        {
            Logger.Log("Translating ProjectDetail to YumlClassOutput", LogLevel.High);

            var parentDiagram = MakeParentDiagram(projectDetail, newlineForEachRelationship);
            return new YumlClassOutput(MakeDependenciesDiagram(projectDetail, newlineForEachRelationship), parentDiagram, projectDetail.FullPath);
        }

        private YumlClassDiagram MakeDependenciesDiagram(ProjectDetail projectDetail, bool newlineForEachRelationship)
        {
            var classDiagram = new YumlClassDiagram(newlineForEachRelationship);

            GenerateDependencyDiagram(projectDetail, classDiagram.Relationships, newlineForEachRelationship);
            return classDiagram;
        }

        private YumlClassDiagram MakeParentDiagram(ProjectDetail projectDetail, bool newlineForEachRelationship)
        {
            var classDiagram = new YumlClassDiagram(newlineForEachRelationship);

            GenerateParentDiagram(projectDetail, classDiagram.Relationships, newlineForEachRelationship);
            return classDiagram;
        }

        private void GenerateDependencyDiagram(ProjectDetail projectDetail, ISet<YumlRelationshipBase> existingRelationships, bool newlineForEachRelationship)
        {
            existingRelationships.UnionWith(MakeYumlDependencies(projectDetail));

            foreach (var linkObject in projectDetail.ChildProjects)
            {
                var project = ProjectRepository.GetById(linkObject.Id);
                if (null != project)
                {
                    GenerateDependencyDiagram(project, existingRelationships, newlineForEachRelationship);
                }
            }
        }

        private YumlClassDiagram MakeDependenciesDiagram(RootNode rootNode, bool newlineForEachRelationship)
        {
            var classDiagram = new YumlClassDiagram(newlineForEachRelationship);
            foreach (var detail in rootNode.ChildProjects)
            {
                classDiagram.Relationships.UnionWith(MakeYumlDependencies(detail));
            }
            return classDiagram;
        }

        private ISet<YumlRelationshipBase> MakeYumlDependencies(ProjectDetail projectDetail)
        {
            var relationships = new HashSet<YumlRelationshipBase>();
            var detailModel = MakeClass(projectDetail);

            foreach (var linkObject in projectDetail.ChildProjects)
            {
                var childProjectDetail = ProjectRepository.GetById(linkObject.Id);

                _ = relationships.Add(new SimpleAssociation(detailModel, MakeClass(childProjectDetail, linkObject)));
            }

            foreach (var dllReference in projectDetail.References)
            {
                var childModel = MakeClass(dllReference);
                _ = relationships.Add(new SimpleAssociation(detailModel, childModel));
            }

            return relationships;
        }

        private YumlClassWithDetails MakeClass(ProjectDetail childProjectDetail, ProjectLinkObject linkObject)
        {
            if (null != childProjectDetail)
            {
                return MakeClass(childProjectDetail);
            }
            else
            {
                return MakeClass(linkObject);
            }
        }

        private void GenerateParentDiagram(ProjectDetail projectDetail, ISet<YumlRelationshipBase> existingRelationships, bool newlineForEachRelationship)
        {
            existingRelationships.UnionWith(MakeYumlParents(projectDetail));

            foreach (var linkObject in projectDetail.ParentProjects)
            {
                GenerateParentDiagram(ProjectRepository.GetById(linkObject.Id), existingRelationships, newlineForEachRelationship);
            }
        }

        private ISet<YumlRelationshipBase> MakeYumlParents(ProjectDetail projectDetail)
        {
            var relationships = new HashSet<YumlRelationshipBase>();
            var detailModel = MakeClass(projectDetail);

            foreach (var linkObject in projectDetail.ParentProjects)
            {
                var parentModel = MakeClass(ProjectRepository.GetById(linkObject.Id));
                _ = relationships.Add(new SimpleAssociation(parentModel, detailModel));
            }

            foreach (var dllReference in projectDetail.References)
            {
                var parentModel = MakeClass(dllReference);
                _ = relationships.Add(new SimpleAssociation(parentModel, detailModel));
            }

            return relationships;
        }

        private static YumlClassWithDetails MakeClass(ProjectLinkObject projectLink)
        {
            return new YumlClassWithDetails(projectLink.FullPath);
        }

        private static YumlClassWithDetails MakeClass(ProjectDetail projectDetail)
        {
            var detailModel = new YumlClassWithDetails(projectDetail.Name);

            AddDotNetNotes(projectDetail, detailModel);
            AddCppNotes(projectDetail.CppDetails, detailModel);

            return detailModel;
        }

        private static void AddDotNetNotes(ProjectDetail projectDetail, YumlClassWithDetails detailModel)
        {
            if (!string.IsNullOrEmpty(projectDetail.DotNetVersion))
            {
                detailModel.Notes.Add(".Net Version: " + projectDetail.DotNetVersion);
            }
            else if (projectDetail.Type == ProjectType.CSharp)
            {
                detailModel.Notes.Add("C#");
            }
        }

        private static void AddCppNotes(CppProjectDetails details, YumlClassWithDetails detailModel)
        {
            if (null != details)
            {
                detailModel.Notes.Add("Type: " + details.Type);
                if (!string.IsNullOrWhiteSpace(details.StandardVersion))
                {
                    detailModel.Notes.Add("C++ version: " + details.StandardVersion);
                }
                detailModel.Notes.Add("Is MFC: " + details.IsMfc);
                detailModel.Notes.Add("Is C++/CLI: " + details.IsManaged);
            }
        }

        private static YumlClassWithDetails MakeClass(DllReference dllReference)
        {
            var detailModel = new YumlClassWithDetails(dllReference.AssemblyName);

            detailModel.Notes.Add(
                string.Format("External Reference{0}",
                string.IsNullOrWhiteSpace(dllReference.Version) ? "" : string.Format(" ({0})", dllReference.Version)
                )
            );

            return detailModel;
        }

        private ProjectDetailRepository ProjectRepository { get; }
    }
}
