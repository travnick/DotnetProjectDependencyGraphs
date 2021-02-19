using System;
using System.Collections;
using System.IO;
using System.Xml;

namespace ProjectReferences.Models
{
    public sealed class ProjectFile
    {
        public ProjectFile(string projectPath)
        {
            if (!File.Exists(projectPath))
            {
                throw new FileNotFoundException(projectPath);
            }

            file = new XmlDocument();
            nsManager = new XmlNamespaceManager(file.NameTable);

            nsManager.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003");
            file.Load(projectPath);
        }

        internal XmlNodeList SelectNodes(string path)
        {
            return file.SelectNodes(path, nsManager);
        }

        internal XmlNode SelectSingleNode(string path)
        {
            return file.SelectSingleNode(path, nsManager);
        }

        internal readonly XmlNamespaceManager nsManager;
        private readonly XmlDocument file;
    }
}
