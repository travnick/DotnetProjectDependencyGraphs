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

        public ProjectDetail(string fullFilePath, Guid projectGuidInSolution, XmlNamespaceManager nsMgr, XmlDocument projectFile, bool includeExternalReferences)
        {
            FullPath = fullFilePath;
            ParentProjects = new HashSet<ProjectLinkObject>();
            Type = GetProjectType(fullFilePath);

            FillUpExtraInformation(Type, projectGuidInSolution, nsMgr, projectFile);

            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = Path.GetFileNameWithoutExtension(fullFilePath);
            }

            ChildProjects = GetProjectsDependencies(FullPath, projectFile, nsMgr);

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
                    ParentProjects.Add(link);
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
                FetchCppExtraInfo(nsMgr, xmlDoc);
            }

            var guidNode = xmlDoc.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:ProjectGuid", nsMgr);
            if (null != guidNode)
            {
                var projectGuid = Guid.Parse(guidNode.InnerText);
                if (projectGuid != projectGuidInSolution && projectGuidInSolution != new Guid())
                {
                    throw new ArgumentException(String.Format("Guid in soludion '{0}' and in project file '{1}' does not match", projectGuidInSolution, projectGuid));
                }
                Id = projectGuid;
            }
            else
            {
                Id = projectGuidInSolution;
            }
        }

        private void FetchCppExtraInfo(XmlNamespaceManager nsMgr, XmlDocument xmlDoc)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = GetCppProjectName(nsMgr, xmlDoc);
            }

            var details = new CppProjectDetails
            {
                Type = GetCppProjectType(nsMgr, xmlDoc),
                StandardVersion = GetCppStandardVersion(nsMgr, xmlDoc),
                IsMfc = IsMfcProject(nsMgr, xmlDoc),
                IsManaged = IsManaged(nsMgr, xmlDoc),
            };


            CppDetails = details;
        }

        private string GetCppProjectType(XmlNamespaceManager nsMgr, XmlDocument xmlDoc)
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

        private static ProjectDetail.ProjectType GetProjectType(string fullFilePath)
        {
            if (fullFilePath.EndsWith(".csproj"))
            {
                return ProjectDetail.ProjectType.CSharp;
            }
            else if (fullFilePath.EndsWith(".vcxproj"))
            {
                return ProjectDetail.ProjectType.Cpp;
            }
            else
            {
                return ProjectDetail.ProjectType.Other;
            }
        }

        private ISet<ProjectLinkObject> GetProjectsDependencies(string projectPath, XmlDocument projectFile, XmlNamespaceManager nsMgr)
        {
            var references = GetProjectReferences(projectPath, projectFile, nsMgr);

            references.UnionWith(GetLibrariesReferences(projectPath, projectFile, nsMgr));

            return references;
        }

        private static ISet<ProjectLinkObject> GetProjectReferences(string projectPath, XmlDocument projectFile, XmlNamespaceManager nsMgr)
        {
            DirectoryInfo projectDirectory = new FileInfo(projectPath).Directory;

            XmlNodeList projectReferences = projectFile.SelectNodes(@"/msb:Project/msb:ItemGroup/msb:ProjectReference", nsMgr);
            var projectReferenceObjects = new HashSet<ProjectLinkObject>();

            foreach (XmlElement reference in projectReferences)
            {
                string subProjectPath = Path.GetFullPath(Path.Combine(projectDirectory.FullName, reference.GetAttribute("Include")));
                var id = new Guid(reference.SelectSingleNode("msb:Project", nsMgr).InnerText);

                var projectLinkObject = new ProjectLinkObject(subProjectPath, id);

                projectReferenceObjects.Add(projectLinkObject);
            }

            return projectReferenceObjects;
        }

        private static ISet<ProjectLinkObject> GetLibrariesReferences(string projectPath, XmlDocument projectFile, XmlNamespaceManager nsMgr)
        {
            XmlNodeList libReferences = projectFile.SelectNodes(@"/msb:Project/msb:ItemDefinitionGroup/msb:Link/msb:AdditionalDependencies", nsMgr);
            var projectReferenceObjects = new HashSet<ProjectLinkObject>();

            foreach (XmlElement reference in libReferences)
            {
                var libraryNames = reference.InnerText.Split(';').ToList();
                libraryNames.Remove("%(AdditionalDependencies)");
                libraryNames.Remove("");

                foreach (var libraryName in libraryNames)
                {
                    var projectLinkObject = ProjectLinkObject.MakeOutOfSolutionLink(libraryName);

                    if (!projectReferenceObjects.Contains(projectLinkObject))
                    {
                        projectReferenceObjects.Add(projectLinkObject);
                    }
                }
            }

            return projectReferenceObjects;
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
                var include = reference.GetAttribute("Include");
                var version = reference.GetAttribute("Version");

                //if not version stored as XML then try and get the hint path.
                if (string.IsNullOrWhiteSpace(version))
                {
                    string relativeHintPath = GetHintPath(reference.InnerXml);

                    if (!string.IsNullOrWhiteSpace(relativeHintPath))
                    {
                        var csprojFile = new FileInfo(fullFilePath);

                        var directory = csprojFile.Directory.FullName + (relativeHintPath.StartsWith(@"\") ? "" : @"\");
                        var dllPath = Path.GetFullPath(directory + relativeHintPath);
                        var dllFile = new FileInfo(dllPath);
                        if (dllFile.Exists)
                        {
                            Assembly assembly = Assembly.LoadFrom(dllFile.FullName);
                            Version ver = assembly.GetName().Version;
                            version = ver.ToString();
                        }
                    }
                }

                dllReferenceObjects.Add(new DllReference { AssemblyName = include.Split(',')[0], Version = version });
            }

            return dllReferenceObjects;
        }

        private static string GetHintPath(string inner)
        {
            if (!string.IsNullOrWhiteSpace(inner) && inner.StartsWith("<HintPath"))
            {
                var tagEndString = "</HintPath>";
                var tagEnd = inner.IndexOf(tagEndString);
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
