using YumlOutput.Class;

namespace ProjectReferences.Output.Yuml.Models
{
    public sealed class YumlClassOutput
    {
        public YumlClassOutput(YumlClassDiagram dependenciesDiagram, YumlClassDiagram parentDiagram, string rootFile)
        {
            DependencyDiagram = dependenciesDiagram;
            ParentDiagram = parentDiagram;
            RootFile = rootFile;
        }
        public string RootFile { get; private set; }
        public YumlClassDiagram DependencyDiagram { get; private set; }
        public YumlClassDiagram ParentDiagram { get; private set; }
    }
}