using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace ProjectReferences.Models
{
    public sealed class ProjectDetail : IEquatable<ProjectDetail>
    {
        public enum ProjectType
        {
            CSharp,
            Cpp,
            Other
        }

        public Guid Id { get; private set; }

        public string FullPath { get; private set; }

        public ProjectType Type { get; private set; }

        public string DotNetVersion { get; private set; }

        public CppProjectDetails CppDetails { get; private set; }

        public string Name { get; private set; }

        public void OptimizeReferences(ProjectDetailRepository projectRepository)
        {
            OptimizeCollection(projectRepository, ChildProjects);
            OptimizeCollection(projectRepository, ParentProjects);
        }

        private static void OptimizeCollection(ProjectDetailRepository projectRepository, ISet<ProjectLinkObject> references)
        {
            var projectsToRemove = new HashSet<ProjectLinkObject>();
            var projectsToAdd = new HashSet<ProjectLinkObject>();

            foreach (var reference in references)
            {
                if (reference.IsOutOfSolution)
                {
                    string name = Path.GetFileNameWithoutExtension(reference.FullPath);

                    var projects = projectRepository.GetByName(name);

                    if (projects.Count == 0)
                    {
                        // Just leave it as is
                    }
                    else if (projects.Count == 1)
                    {
                        var projectLinkObject = new ProjectLinkObject(projects.First());

                        _ = projectsToRemove.Add(reference);
                        _ = projectsToAdd.Add(projectLinkObject);
                    }
                    else
                    {
                        throw new ArgumentException(string.Format("More than one project has the same name '{0}'", name));
                    }
                }
            }

            references.ExceptWith(projectsToRemove);
            references.UnionWith(projectsToAdd);
        }

        public ISet<ProjectLinkObject> ChildProjects { get; private set; }

        public ISet<ProjectLinkObject> ParentProjects { get; private set; }

        public ISet<DllReference> References { get; private set; }

        public ProjectDetail(ProjectDetailRepository projectRepository, string fullFilePath, Guid projectGuidInSolution, XmlNamespaceManager nsMgr, XmlDocument projectFile, bool includeExternalReferences)
        {
            FullPath = fullFilePath;
            ParentProjects = new HashSet<ProjectLinkObject>();
            Type = GetProjectType(fullFilePath);

            FillUpExtraInformation(Type, projectGuidInSolution, nsMgr, projectFile);

            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = Path.GetFileNameWithoutExtension(fullFilePath);
            }

            ChildProjects = GetDependencies(projectRepository, FullPath, projectFile, nsMgr);

            if (includeExternalReferences)
            {
                References = GetExternalReferences(fullFilePath, projectFile, nsMgr);
            }
            else
            {
                References = new HashSet<DllReference>();
            }
        }

        public void AddParentLinks(ISet<ProjectLinkObject> parentLinks)
        {
            foreach (var link in parentLinks)
            {
                if (!ParentProjects.Any(p => p.Id.Equals(link.Id)))
                {
                    _ = ParentProjects.Add(link);
                }
            }
        }

        private void FillUpExtraInformation(ProjectType projectType, Guid projectGuidInSolution, XmlNamespaceManager nsMgr, XmlDocument xmlDoc)
        {
            if (projectType == ProjectType.CSharp)
            {
                FetchCSharpExtraInfo(nsMgr, xmlDoc);
            }
            else if (projectType == ProjectType.Cpp)
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    Name = GetCppProjectName(nsMgr, xmlDoc);
                }

                CppDetails = GetCppExtraInfo(nsMgr, xmlDoc);
            }

            Id = GetGuid(projectGuidInSolution, nsMgr, xmlDoc);
        }

        private static Guid GetGuid(Guid projectGuidInSolution, XmlNamespaceManager nsMgr, XmlDocument xmlDoc)
        {
            var guidNode = xmlDoc.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:ProjectGuid", nsMgr);
            if (null != guidNode)
            {
                var projectGuid = Guid.Parse(guidNode.InnerText);
                if (projectGuid != projectGuidInSolution && projectGuidInSolution != new Guid())
                {
                    throw new ArgumentException(string.Format("Guid in soludion '{0}' and in project file '{1}' does not match", projectGuidInSolution, projectGuid));
                }
                return projectGuid;
            }
            else
            {
                return projectGuidInSolution;
            }
        }

        private static CppProjectDetails GetCppExtraInfo(XmlNamespaceManager nsMgr, XmlDocument xmlDoc)
        {
            var details = new CppProjectDetails
            {
                Type = GetCppProjectType(nsMgr, xmlDoc),
                StandardVersion = GetCppStandardVersion(nsMgr, xmlDoc),
                IsMfc = IsMfcProject(nsMgr, xmlDoc),
                IsManaged = IsManaged(nsMgr, xmlDoc),
            };

            return details;
        }

        private static string GetCppProjectType(XmlNamespaceManager nsMgr, XmlDocument xmlDoc)
        {
            var configurationType = xmlDoc.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:ConfigurationType", nsMgr);
            if (null != configurationType)
            {
                return configurationType.InnerText;
            }

            return "!! Unknown !!";
        }

        private static bool IsManaged(XmlNamespaceManager nsMgr, XmlDocument xmlDoc)
        {
            var keywordNode = xmlDoc.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:Keyword", nsMgr);
            if (null != keywordNode)
            {
                return keywordNode.InnerText == "ManagedCProj";
            }

            return false;
        }

        private static bool IsMfcProject(XmlNamespaceManager nsMgr, XmlDocument xmlDoc)
        {
            var useOfMfcNode = xmlDoc.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:UseOfMfc", nsMgr);
            if (null != useOfMfcNode)
            {
                return useOfMfcNode.InnerText == "Dynamic" || useOfMfcNode.InnerText == "Static";
            }

            return false;
        }

        private static string GetCppStandardVersion(XmlNamespaceManager nsMgr, XmlDocument xmlDoc)
        {
            var languageStandardNode = xmlDoc.SelectSingleNode(@"/msb:Project/msb:ItemDefinitionGroup/msb:ClCompile/msb:LanguageStandard", nsMgr);
            if (null != languageStandardNode)
            {
                return languageStandardNode.InnerText;
            }
            else
            {
                return "";
            }
        }

        private static string GetCppProjectName(XmlNamespaceManager nsMgr, XmlDocument xmlDoc)
        {
            var projectNameNode = xmlDoc.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:ProjectName", nsMgr);
            if (null != projectNameNode)
            {
                return projectNameNode.InnerText;
            }
            else
            {
                var rootNamespaceNode = xmlDoc.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:ProjectName", nsMgr);
                if (null != rootNamespaceNode)
                {
                    return rootNamespaceNode.InnerText;
                }
                else
                {
                    return "";
                }
            }
        }

        private void FetchCSharpExtraInfo(XmlNamespaceManager nsMgr, XmlDocument xmlDoc)
        {
            var nameNode = xmlDoc.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:AssemblyName", nsMgr);
            if (null != nameNode)
            {
                Name = nameNode.InnerText;
            }

            var dotNetVersionNode = xmlDoc.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:TargetFrameworkVersion", nsMgr);
            if (null != dotNetVersionNode)
            {
                DotNetVersion = dotNetVersionNode.InnerText;
            }
        }

        private static ProjectType GetProjectType(string fullFilePath)
        {
            if (fullFilePath.EndsWith(".csproj"))
            {
                return ProjectType.CSharp;
            }
            else if (fullFilePath.EndsWith(".vcxproj"))
            {
                return ProjectType.Cpp;
            }
            else
            {
                return ProjectType.Other;
            }
        }

        private ISet<ProjectLinkObject> GetDependencies(ProjectDetailRepository projectRepository, string projectPath, XmlDocument projectFile, XmlNamespaceManager nsMgr)
        {
            var references = GetProjectReferences(projectPath, projectFile, nsMgr);

            MergeReferencesWith(references, GetRawLibrariesReferences(projectRepository, projectFile, nsMgr));

            return references;
        }

        private static ISet<ProjectLinkObject> GetProjectReferences(string projectPath, XmlDocument projectFile, XmlNamespaceManager nsMgr)
        {
            var projectDirectory = new FileInfo(projectPath).Directory;

            var projectReferences = projectFile.SelectNodes(@"/msb:Project/msb:ItemGroup/msb:ProjectReference", nsMgr);
            var projectReferenceObjects = new HashSet<ProjectLinkObject>();

            foreach (XmlElement reference in projectReferences)
            {
                string subProjectPath = Path.GetFullPath(Path.Combine(projectDirectory.FullName, reference.GetAttribute("Include")));
                var id = new Guid(reference.SelectSingleNode("msb:Project", nsMgr).InnerText);

                var projectLinkObject = new ProjectLinkObject(subProjectPath, id);

                _ = projectReferenceObjects.Add(projectLinkObject);
            }

            return projectReferenceObjects;
        }

        private static ISet<ProjectLinkObject> GetRawLibrariesReferences(ProjectDetailRepository projectRepository, XmlDocument projectFile, XmlNamespaceManager nsMgr)
        {
            var projectReferenceObjects = new HashSet<ProjectLinkObject>();

            {
                var libReferences = projectFile.SelectNodes(@"/msb:Project/msb:ItemDefinitionGroup/msb:Link/msb:AdditionalDependencies", nsMgr);
                var libsToIgnore = new HashSet<string> { "%(AdditionalDependencies)" };

                projectReferenceObjects.UnionWith(GetRawLibrariesReferencesInNode(libReferences, libsToIgnore));
            }

            {
                var libReferences = projectFile.SelectNodes(@"/msb:Project/msb:ItemDefinitionGroup/msb:Link/msb:DelayLoadDLLs", nsMgr);
                var libsToIgnore = new HashSet<string> { "%(DelayLoadDLLs)" };

                projectReferenceObjects.UnionWith(GetRawLibrariesReferencesInNode(libReferences, libsToIgnore));
            }

            OptimizeCollection(projectRepository, projectReferenceObjects);

            return projectReferenceObjects;
        }

        private static ISet<ProjectLinkObject> GetRawLibrariesReferencesInNode(XmlNodeList references, ISet<string> libsToIgnore)
        {
            var projectReferenceObjects = new HashSet<ProjectLinkObject>();

            foreach (XmlElement reference in references)
            {
                var libraryNames = new HashSet<string>(reference.InnerText.Split(';').ToList());

                libraryNames.ExceptWith(libsToIgnore);
                _ = libraryNames.Remove("");

                foreach (string libraryName in libraryNames)
                {
                    var projectLinkObject = ProjectLinkObject.MakeOutOfSolutionLink(libraryName);

                    _ = projectReferenceObjects.Add(projectLinkObject);
                }
            }

            return projectReferenceObjects;
        }

        private static void MergeReferencesWith(ISet<ProjectLinkObject> currentReferences, ISet<ProjectLinkObject> delayLoadedLibraries)
        {
            foreach (var newDependency in delayLoadedLibraries)
            {
                string name = Path.GetFileNameWithoutExtension(newDependency.FullPath);
                var alternativeNames = new List<ProjectLinkObject> { new ProjectLinkObject(name), new ProjectLinkObject(name + ".lib"), newDependency };
                bool alreadyExists = false;

                foreach (var alternativeName in alternativeNames)
                {
                    if (currentReferences.Contains(alternativeName))
                    {
                        alreadyExists |= true;
                    }
                }

                if (!alreadyExists)
                {
                    _ = currentReferences.Add(newDependency);
                }
            }
        }

        private static ISet<DllReference> GetExternalReferences(string fullFilePath, XmlDocument projectFile, XmlNamespaceManager nsMgr)
        {
            //get all dll references
            IEnumerable dllReferences = projectFile.SelectNodes(@"/msb:Project/msb:ItemGroup/msb:Reference[not(starts-with(@Include,'System')) and not(starts-with(@Include,'Microsoft.'))]", nsMgr);
            var dllReferenceObjects = new HashSet<DllReference>();

            if (dllReferences == null)
            {
                return dllReferenceObjects;
            }

            foreach (XmlElement reference in dllReferences ?? Enumerable.Empty<XmlElement>())
            {
                string include = reference.GetAttribute("Include");
                string version = reference.GetAttribute("Version");

                //if not version stored as XML then try and get the hint path.
                if (string.IsNullOrWhiteSpace(version))
                {
                    string relativeHintPath = GetHintPath(reference.InnerXml);

                    if (!string.IsNullOrWhiteSpace(relativeHintPath))
                    {
                        var csprojFile = new FileInfo(fullFilePath);

                        string directory = csprojFile.Directory.FullName + (relativeHintPath.StartsWith(@"\") ? "" : @"\");
                        string dllPath = Path.GetFullPath(directory + relativeHintPath);
                        var dllFile = new FileInfo(dllPath);
                        if (dllFile.Exists)
                        {
                            var assembly = Assembly.LoadFrom(dllFile.FullName);
                            var ver = assembly.GetName().Version;
                            version = ver.ToString();
                        }
                    }
                }

                _ = dllReferenceObjects.Add(new DllReference { AssemblyName = include.Split(',')[0], Version = version });
            }

            return dllReferenceObjects;
        }

        private static string GetHintPath(string inner)
        {
            if (!string.IsNullOrWhiteSpace(inner) && inner.StartsWith("<HintPath"))
            {
                string tagEndString = "</HintPath>";
                int tagEnd = inner.IndexOf(tagEndString);
                if (tagEnd > 0)
                {
                    inner = inner.Substring(0, tagEnd + tagEndString.Length);

                    var innerHintPathXml = new XmlDocument();
                    innerHintPathXml.LoadXml(inner);

                    return innerHintPathXml.InnerText;
                }
            }

            return null;
        }

        public bool Equals(ProjectDetail other)
        {
            return Id == other.Id;
        }
    }
}
