using System;
using System.Collections.Generic;
using ProjectReferences.Models;
using ProjectReferences.Output.Yuml.Models;
using ProjectReferences.Shared;
using YumlOutput.Class;
using YumlOutput.Class.Models;
using YumlOutput.Class.Relationships;

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
            existingRelationships.UnionWith(MakeYumlDependencies(projectDetail, newlineForEachRelationship));

            foreach (var linkObject in projectDetail.ChildProjects)
            {
                GenerateDependencyDiagram(ProjectRepository.GetById(linkObject.Id), existingRelationships, newlineForEachRelationship);
            }
        }

        private YumlClassDiagram MakeDependenciesDiagram(RootNode rootNode, bool newlineForEachRelationship)
        {
            var classDiagram = new YumlClassDiagram(newlineForEachRelationship);
            foreach (var detail in rootNode.ChildProjects)
            {
                classDiagram.Relationships.UnionWith(MakeYumlDependencies(detail, newlineForEachRelationship));
            }
            return classDiagram;
        }

        private ISet<YumlRelationshipBase> MakeYumlDependencies(ProjectDetail projectDetail, bool newlineForEachRelationship)
        {
            var relationships = new HashSet<YumlRelationshipBase>();
            var detailModel = MakeClass(projectDetail);

            foreach (var linkObject in projectDetail.ChildProjects)
            {
                var childModel = MakeClass(ProjectRepository.GetById(linkObject.Id));
                relationships.Add(new SimpleAssociation(detailModel, childModel));
            }

            foreach (var dllReference in projectDetail.References)
            {
                var childModel = MakeClass(dllReference);
                relationships.Add(new SimpleAssociation(detailModel, childModel));
            }

            return relationships;
        }

        private void GenerateParentDiagram(ProjectDetail projectDetail, ISet<YumlRelationshipBase> existingRelationships, bool newlineForEachRelationship)
        {
            existingRelationships.UnionWith(MakeYumlParents(projectDetail, newlineForEachRelationship));

            foreach (var linkObject in projectDetail.ParentProjects)
            {
                GenerateParentDiagram(ProjectRepository.GetById(linkObject.Id), existingRelationships, newlineForEachRelationship);
            }
        }

        private ISet<YumlRelationshipBase> MakeYumlParents(ProjectDetail projectDetail, bool newlineForEachRelationship)
        {
            var relationships = new HashSet<YumlRelationshipBase>();
            var detailModel = MakeClass(projectDetail);

            foreach (var linkObject in projectDetail.ParentProjects)
            {
                var parentModel = MakeClass(ProjectRepository.GetById(linkObject.Id));
                relationships.Add(new SimpleAssociation(parentModel, detailModel));
            }

            foreach (var dllReference in projectDetail.References)
            {
                var parentModel = MakeClass(dllReference);
                relationships.Add(new SimpleAssociation(parentModel, detailModel));
            }

            return relationships;
        }

        static private YumlClassWithDetails MakeClass(ProjectDetail projectDetail)
        {
            var detailModel = new YumlClassWithDetails();
            detailModel.Name = projectDetail.Name;

            if (!string.IsNullOrEmpty(projectDetail.DotNetVersion))
            {
                detailModel.Notes.Add(".Net Version: " + projectDetail.DotNetVersion);
            }

            return detailModel;
        }

        static private YumlClassWithDetails MakeClass(DllReference dllReference)
        {
            var detailModel = new YumlClassWithDetails();
            detailModel.Name = dllReference.AssemblyName;// string.Format("{0}.dll", dllReference.AssemblyName);
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
