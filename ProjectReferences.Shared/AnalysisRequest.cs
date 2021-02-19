using System.Collections.Generic;
using System.IO;

namespace ProjectReferences.Shared
{
    public sealed class AnalysisRequest
    {
        public AnalysisRequest()
        {
            NumberOfLevelsToDig = int.MaxValue;
            ExcludeNames = new List<string>();
            CreateOutputForEachItem = false;
            OutputType = OutputType.YumlReferenceList;
            LogLevel = LogLevel.Low;
            LogType = LogType.Console;
            IncludeExternalReferences = false;
        }

        public string RootFile
        {
            get
            {
                return _rootFile;
            }
            set
            {
                _rootFile = value;

                if (string.IsNullOrWhiteSpace(_outputFolder))
                {
                    _outputFolder = Path.GetFileNameWithoutExtension(value);
                }

            }
        }

        public ISet<string> MergeWith = new HashSet<string>();

        public int NumberOfLevelsToDig { get; set; }

        public IList<string> ExcludeNames { get; set; }

        public string OutputFolder
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_outputFolder))
                {
                    return @"ProjectDependenciesOutput";
                }
                else
                {
                    return _outputFolder;
                }
            }

            set => _outputFolder = value;
        }

        public bool CreateOutputForEachItem { get; set; }

        public OutputType OutputType { get; set; }

        public bool IncludeExternalReferences { get; set; }

        public LogLevel LogLevel { get; set; }

        public LogType LogType { get; set; }

        public string LogOutputFolderLocation { get; set; }

        public string LogOutputFileLocation { get; set; }

        private string _rootFile;
        private string _outputFolder;
    }
}
