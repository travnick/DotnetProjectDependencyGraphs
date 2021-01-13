using System.Collections.Generic;

namespace ProjectReferences.Shared
{
    public sealed class AnalysisRequest
    {
        public AnalysisRequest()
        {
            NumberOfLevelsToDig = int.MaxValue;
            OutputFolder = @"ProjectDependenciesOutput";
            ExcludeNames = new List<string>();
            CreateOutputForEachItem = false;
            OutputType = OutputType.YumlReferenceList;
            LogLevel = LogLevel.Low;
            LogType = LogType.Console;
            IncludeExternalReferences = false;
        }

        public string RootFile { get; set; }

        public int NumberOfLevelsToDig { get; set; }

        public IList<string> ExcludeNames { get; protected set; }

        public string OutputFolder { get; set; }

        public bool CreateOutputForEachItem { get; set; }

        public OutputType OutputType { get; set; }

        public bool IncludeExternalReferences { get; set; }

        public LogLevel LogLevel { get; set; }

        public LogType LogType { get; set; }

        public string LogOutputFolderLocation { get; set; }

        public string LogOutputFileLocation { get; set; }
    }
}
