using System;
using System.Collections.Generic;
using Mono.Options;
using ProjectReferences.Shared;

namespace ProjectReferences.App
{
    /*
     * Example of command line args
     *
     * --root-file "D:\Work\Aerdata\StreamInteractive\Dev-2.6\Shared\Stream2.JobQueuePersistence\Stream2.JobQueuePersistence.csproj"
     * --output-folder "C:\temp\projectReferences"
     * --output-each-item
     * --output-type YumlReferenceList
     * --log-level High
     */
    internal class CommandLineArgs
    {
        public AnalysisRequest Process(string[] args)
        {
            var request = new AnalysisRequest();
            return Parse(request, args);
        }

        public AnalysisRequest Parse(AnalysisRequest request, string[] args)
        {
            AddOptions(request);

            try
            {
                var extras = _options.Parse(args);

                if (extras.Count > 0)
                {
                    Console.WriteLine("There are not recognized arguments: '{0}'\n", string.Join("', '", extras));

                    PrintHelp();

                    return null;
                }

                if (_printHelp || !VerifyRequiredOptions())
                {
                    PrintHelp();

                    return null;
                }

                // if the output type is OutPutType.HtmlDocument then it needs to produce png's for each item.
                // So set the output each item flag to true, regardless of params
                if (request.OutputType == OutputType.HtmlDocument)
                {
                    request.CreateOutputForEachItem = true;
                }

                return request;
            }
            catch (OptionException e)
            {
                Console.WriteLine(string.Format("Cannot parse the arguments: ", e.Message));

                PrintHelp();

                return null;
            }
        }

        private void AddOptions(AnalysisRequest request)
        {
            AddOption("h|help", "Prints this hellp",
                option => _printHelp = null != option);

            AddRequired("root-file=", "File to generate references of",
                n => request.RootFile = n);

            AddOption("output-each-item", "Generates dependencies for each item",
                n => request.CreateOutputForEachItem = null != n);

            AddOptional("output-type=", "Output type",
                n => request.OutputType = (OutputType)Enum.Parse(typeof(OutputType), n, true));

            AddOptional("level-to-dig=", "How deep to dig in references",
                n => request.NumberOfLevelsToDig = int.Parse(n));

            AddOption("include-external", "Include external references",
                option => request.IncludeExternalReferences = null != option);

            AddOptional("output-folder=", "Directory path to put output in",
                n => request.OutputFolder = n);

            AddOptional("log-level=", "Log level",
                n => request.LogLevel = (LogLevel)Enum.Parse(typeof(LogLevel), n, true));

            AddOptional("log-folder=", "Log directory",
                n => request.LogOutputFolderLocation = n);

            AddOptional("log-file=", "Log file",
                n => request.LogOutputFileLocation = n);

            AddOptional("log-type=", "Log type",
                n => request.LogType = (LogType)Enum.Parse(typeof(LogType), n, true));
        }

        private void AddOption(string prototype, string description, Action<string> action, bool hidden = false)
        {
            _ = _options.Add(new ActionOption(prototype, description, 0, action, hidden));
        }

        private void AddRequired(string prototype, string description, Action<string> action, bool hidden = false)
        {
            AddRequired(prototype, description, 1, action, hidden);
        }
        private void AddRequired(string prototype, string description, int count, Action<string> action, bool hidden = false)
        {
            var option = new ActionOption(prototype, description, count, action, hidden);

            _ = _options.Add(option);

            _requiredOptions.Add(option);
        }

        private void AddOptional(string prototype, string description, Action<string> action, bool hidden = false)
        {
            AddOptional(prototype, description, 1, action, hidden);
        }

        private void AddOptional(string prototype, string description, int count, Action<string> action, bool hidden = false)
        {
            _ = _options.Add(new ActionOption(prototype, description, count, action, hidden));
        }

        private void PrintHelp()
        {
            Console.WriteLine("Available options:");
            _options.WriteOptionDescriptions(Console.Out);
        }

        private bool VerifyRequiredOptions()
        {
            bool allValid = true;

            foreach (var option in _requiredOptions)
            {
                if (!option.Called)
                {
                    allValid = false;
                    Console.WriteLine(string.Format("There is missing required option: '{0}'\n", option.GetNames()));
                }
            }

            return allValid;
        }

        private readonly IList<ActionOption> _requiredOptions = new List<ActionOption>();
        private readonly OptionSet _options = new OptionSet();
        private bool _printHelp;
    }
}
