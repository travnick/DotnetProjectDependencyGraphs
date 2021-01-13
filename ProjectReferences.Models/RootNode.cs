using System.IO;

namespace ProjectReferences.Models
{
    public sealed class RootNode
    {
        public RootNode()
        {
            ChildProjects = new ProjectDetailRepository();
        }

        public RootNodeType NodeType { get; set; }
        public FileInfo File { get; set; }
        public string Name { get; set; }
        public DirectoryInfo Directory { get; set; }
        public int SearchDepth { get; set; }

        public ProjectDetailRepository ChildProjects { get; private set; }
    }

    public enum RootNodeType
    {
        SLN = 1,
        CSPROJ = 2,
    }
}
