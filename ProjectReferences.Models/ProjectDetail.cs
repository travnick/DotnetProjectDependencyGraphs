using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace ProjectReferences.Models
{
    public sealed class ProjectDetail: IEquatable<ProjectDetail>
    {
        public enum ProjectType
        {
            CSharp,
            Other
        }

        public ProjectDetail(string fullFilePath, XmlNamespaceManager nsMgr, XmlDocument projectFile, bool includeExternalReferences)
        {
            FullPath = fullFilePath;
            ParentProjects = new HashSet<ProjectLinkObject>();
            Type = GetProjectType(fullFilePath);

            FillUpExtraInformation(fullFilePath, nsMgr, projectFile);
            ChildProjects = GetProjectsDependencies(FullPath, projectFile, nsMgr);

            if (includeExternalReferences)
            {
                References = GetExternalReferences(fullFilePath, includeExternalReferences, projectFile, nsMgr);
            }
            else
            {
                References = new HashSet<DllReference>();
            }
        }

        public Guid Id { get; private set; }

        public string FullPath { get; private set; }

        public string DotNetVersion { get; private set; }

        public string Name { get; private set; }

        public ProjectType Type { get; private set; }

        public ISet<ProjectLinkObject> ChildProjects { get; private set; }

        public ISet<ProjectLinkObject> ParentProjects { get; private set; }

        public ISet<DllReference> References { get; private set; }

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
        private void FillUpExtraInformation(string fullFilePath, XmlNamespaceManager nsMgr, XmlDocument xmlDoc)
        {
            if (Type == ProjectDetail.ProjectType.CSharp)
            {
                Name = xmlDoc.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:AssemblyName", nsMgr).InnerText;
                DotNetVersion = xmlDoc.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:TargetFrameworkVersion", nsMgr).InnerText;
            }
            else
            {
                Name = Path.GetFileNameWithoutExtension(fullFilePath);
            }

            Id = Guid.Parse(xmlDoc.SelectSingleNode(@"/msb:Project/msb:PropertyGroup/msb:ProjectGuid", nsMgr).InnerText);
        }

        private static ProjectDetail.ProjectType GetProjectType(string fullFilePath)
        {
            if (fullFilePath.EndsWith(".csproj"))
            {
                return ProjectDetail.ProjectType.CSharp;
            }
            else
            {
                return ProjectDetail.ProjectType.Other;
            }
        }
        private ISet<ProjectLinkObject> GetProjectsDependencies(string projectPath, XmlDocument projectFile, XmlNamespaceManager nsMgr)
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

        private static ISet<DllReference> GetExternalReferences(string fullFilePath, bool includeExternalReferences, XmlDocument projectFile, XmlNamespaceManager nsMgr)
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
                    var inner = reference.InnerXml;
                    if (!string.IsNullOrWhiteSpace(inner) && inner.StartsWith("<HintPath"))
                    {
                        var innerHintPathXml = new XmlDocument();
                        innerHintPathXml.LoadXml(inner);

                        var relativehintPath = innerHintPathXml.InnerText;
                        var csprojFile = new FileInfo(fullFilePath);

                        var directory = csprojFile.Directory.FullName + (relativehintPath.StartsWith(@"\") ? "" : @"\");
                        var dllPath = Path.GetFullPath(directory + relativehintPath);
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

        public bool Equals(ProjectDetail other)
        {
            return Id == other.Id;
        }
    }
}
