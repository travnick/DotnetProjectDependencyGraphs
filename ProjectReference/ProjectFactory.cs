using System;
using System.IO;
using System.Xml;
using ProjectReferences.Models;

namespace ProjectReference
{
    public sealed class ProjectFactory
    {
        /// <summary>
        /// Creates a project detail object from the path to a CS project file.
        /// </summary>
        /// <param name="fullFilePath"></param>
        /// <returns></returns>
        public static ProjectDetail MakeProjectDetail(string fullFilePath, Guid guid, bool includeExternalReferences)
        {
            if (!File.Exists(fullFilePath))
            {
                throw new FileNotFoundException(fullFilePath);
            }

            //Create an xml doc with correct namespace to analysis project file.
            XmlDocument projectFile = new XmlDocument();
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(projectFile.NameTable);

            nsMgr.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003");
            projectFile.Load(fullFilePath);

            return new ProjectDetail(fullFilePath, guid, nsMgr, projectFile, includeExternalReferences);
        }
    }
}
