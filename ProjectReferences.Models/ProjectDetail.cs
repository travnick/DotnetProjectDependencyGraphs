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

        public ISet<ProjectLinkObject> ChildProjects { get; private set; }

        public ISet<ProjectLinkObject> ParentProjects { get; private set; }

        public ISet<DllReference> References { get; private set; }

        public ProjectDetail(ProjectDetailRepository projectRepository, string fullFilePath, Guid projectGuidInSolution, ProjectFile projectFile, bool includeExternalReferences)
        {
            FullPath = fullFilePath;
            ParentProjects = new HashSet<ProjectLinkObject>();
            Type = GetProjectType(fullFilePath);

            FillUpExtraInformation(Type, projectGuidInSolution, projectFile);

            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = Path.GetFileNameWithoutExtension(fullFilePath);
            }

            ChildProjects = GetDependencies(projectRepository, FullPath, projectFile);

            if (includeExternalReferences)
            {
                References = GetExternalReferences(fullFilePath, projectFile);
            }
            else
            {
                References = new HashSet<DllReference>();
            }
        }

        public void AddParentLinks(ISet<ProjectLinkObject> links)
        {
            ParentProjects.UnionWith(links);
        }

        public void AddChildLinks(ISet<ProjectLinkObject> links)
        {
            ChildProjects.UnionWith(links);
        }

        public void OptimizeReferences(ProjectDetailRepository projectRepository)
        {
            OptimizeCollection(projectRepository, ChildProjects);
            OptimizeCollection(projectRepository, ParentProjects);
        }

        public bool Equals(ProjectDetail other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, FullPath);
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

        private void FillUpExtraInformation(ProjectType projectType, Guid projectGuidInSolution, ProjectFile projectFile)
        {
            if (projectType == ProjectType.CSharp)
            {
                FetchCSharpExtraInfo(projectFile);
            }
            else if (projectType == ProjectType.Cpp)
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    Name = GetCppProjectName(projectFile);
                }

                CppDetails = GetCppExtraInfo(projectFile);
            }

            Id = GetGuid(projectGuidInSolution, projectFile);
        }

        private static Guid GetGuid(Guid projectGuidInSolution, ProjectFile projectFile)
        {
            var guidNode = projectFile.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:ProjectGuid");
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

        private static CppProjectDetails GetCppExtraInfo(ProjectFile projectFile)
        {
            var details = new CppProjectDetails
            {
                Type = GetCppProjectType(projectFile),
                StandardVersion = GetCppStandardVersion(projectFile),
                IsMfc = IsMfcProject(projectFile),
                IsManaged = IsManaged(projectFile),
            };

            return details;
        }

        private static string GetCppProjectType(ProjectFile projectFile)
        {
            var configurationType = projectFile.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:ConfigurationType");
            if (null != configurationType)
            {
                return configurationType.InnerText;
            }

            return "!! Unknown !!";
        }

        private static bool IsManaged(ProjectFile projectFile)
        {
            var keywordNode = projectFile.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:Keyword");
            if (null != keywordNode)
            {
                return keywordNode.InnerText == "ManagedCProj";
            }

            return false;
        }

        private static bool IsMfcProject(ProjectFile projectFile)
        {
            var useOfMfcNode = projectFile.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:UseOfMfc");
            if (null != useOfMfcNode)
            {
                return useOfMfcNode.InnerText == "Dynamic" || useOfMfcNode.InnerText == "Static";
            }

            return false;
        }

        private static string GetCppStandardVersion(ProjectFile projectFile)
        {
            var languageStandardNode = projectFile.SelectSingleNode(@"/msb:Project/msb:ItemDefinitionGroup/msb:ClCompile/msb:LanguageStandard");
            if (null != languageStandardNode)
            {
                return languageStandardNode.InnerText;
            }
            else
            {
                return "";
            }
        }

        private static string GetCppProjectName(ProjectFile projectFile)
        {
            var projectNameNode = projectFile.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:ProjectName");
            if (null != projectNameNode)
            {
                return projectNameNode.InnerText;
            }
            else
            {
                var rootNamespaceNode = projectFile.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:ProjectName");
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

        private void FetchCSharpExtraInfo(ProjectFile projectFile)
        {
            var nameNode = projectFile.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:AssemblyName");
            if (null != nameNode)
            {
                Name = nameNode.InnerText;
            }

            var dotNetVersionNode = projectFile.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:TargetFrameworkVersion");
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

        private ISet<ProjectLinkObject> GetDependencies(ProjectDetailRepository projectRepository, string projectPath, ProjectFile projectFile)
        {
            var references = GetProjectReferences(projectPath, projectFile);

            MergeReferencesWith(references, GetRawLibrariesReferences(projectRepository, projectFile));

            return references;
        }

        private static ISet<ProjectLinkObject> GetProjectReferences(string projectPath, ProjectFile projectFile)
        {
            var projectDirectory = new FileInfo(projectPath).Directory;

            var projectReferences = projectFile.SelectNodes(@"/msb:Project/msb:ItemGroup/msb:ProjectReference");
            var projectReferenceObjects = new HashSet<ProjectLinkObject>();

            foreach (XmlElement reference in projectReferences)
            {
                string subProjectPath = Path.GetFullPath(Path.Combine(projectDirectory.FullName, reference.GetAttribute("Include")));
                var id = new Guid(reference.SelectSingleNode("msb:Project", projectFile.nsManager).InnerText);

                var projectLinkObject = new ProjectLinkObject(subProjectPath, id);

                _ = projectReferenceObjects.Add(projectLinkObject);
            }

            return projectReferenceObjects;
        }

        private static ISet<ProjectLinkObject> GetRawLibrariesReferences(ProjectDetailRepository projectRepository, ProjectFile projectFile)
        {
            var projectReferenceObjects = new HashSet<ProjectLinkObject>();

            {
                var libReferences = projectFile.SelectNodes(@"/msb:Project/msb:ItemDefinitionGroup/msb:Link/msb:AdditionalDependencies");
                var libsToIgnore = new HashSet<string> { "%(AdditionalDependencies)" };

                projectReferenceObjects.UnionWith(GetRawLibrariesReferencesInNode(libReferences, libsToIgnore));
            }

            {
                var libReferences = projectFile.SelectNodes(@"/msb:Project/msb:ItemDefinitionGroup/msb:Link/msb:DelayLoadDLLs");
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

        private static ISet<DllReference> GetExternalReferences(string fullFilePath, ProjectFile projectFile)
        {
            //get all dll references
            IEnumerable dllReferences = projectFile.SelectNodes(@"/msb:Project/msb:ItemGroup/msb:Reference[not(starts-with(@Include,'System')) and not(starts-with(@Include,'Microsoft.'))]");
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
    }
}
