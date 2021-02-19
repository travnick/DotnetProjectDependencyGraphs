using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

using ProjectReference;

using ProjectReferences.Models;
using ProjectReferences.Shared;

namespace ProjectReferences.App
{
    internal sealed class Program
    {
        public static int Main(string[] args)
        {
            Logger.Log("parsing args");
            var request = new CommandLineArgs().Parse(GetAppSettingValues(), args);
            if (null == request)
            {
                return 0;
            }

            Logger.SetupLogger(request);

            //set the current directory to ensure all relative file paths workout correctly.
            _ = new FileInfo(request.RootFile).Directory.FullName;
            //Logger.Log("setting current / working directory to: " + dir);
            //Directory.SetCurrentDirectory(dir);

            Logger.Log("Creating project reference collection for root file: " + request.RootFile);
            var rootNode = Manager.CreateRootNode(request.RootFile, request);

            Logger.Log("Processing rootNode to fill all project references");

            var manager = new Manager();
            manager.Process(rootNode, request.IncludeExternalReferences);

            var mergeWithProjects = MakeMergeWithProjects(manager, request);

            MergeWith(rootNode, mergeWithProjects);

            Logger.Log("Creating output for rootNode", LogLevel.High);
            var outputResponse = Manager.CreateOutput(request, rootNode);

            Logger.Log(string.Format("output creation result: {0}", outputResponse.Success));
            Logger.Log(string.Format("output creation path: {0}", outputResponse.Path));

            return 0;
        }

        private static IList<Models.RootNode> MakeMergeWithProjects(Manager manager, AnalysisRequest request)
        {
            var mergeWithProjects = new List<Models.RootNode>();

            foreach (string mergeWith in request.MergeWith)
            {
                Logger.Log(string.Format("Processing merge with project '{0}'", mergeWith));

                var mergeWithNode = Manager.CreateRootNode(mergeWith, request);
                manager.Process(mergeWithNode, request.IncludeExternalReferences);

                mergeWithProjects.Add(mergeWithNode);
            }

            return mergeWithProjects;
        }

        private static void MergeWith(RootNode rootNode, IList<RootNode> mergeWithProjects)
        {
            foreach (var mergeWithProject in mergeWithProjects)
            {
                Logger.Log(string.Format("Merging with '{0}'", mergeWithProject.Name));

                rootNode.ChildProjects.Merge(mergeWithProject.ChildProjects);
            }
        }

        private static AnalysisRequest GetAppSettingValues()
        {
            var request = new AnalysisRequest();

            string outputFolder = ConfigurationManager.AppSettings["OutputFolder"];
            if (!string.IsNullOrWhiteSpace(outputFolder))
            {
                request.LogOutputFolderLocation = outputFolder;
            }

            string outputFile = ConfigurationManager.AppSettings["OutputFile"];
            if (!string.IsNullOrWhiteSpace(outputFile))
            {
                request.LogOutputFileLocation = outputFile;
            }

            string loggerType = ConfigurationManager.AppSettings["LoggerType"];
            if (!string.IsNullOrWhiteSpace(loggerType))
            {
                if (Enum.IsDefined(typeof(LogType), loggerType))
                {
                    request.LogType = (LogType)Enum.Parse(typeof(LogType), loggerType, true);
                }
            }

            string loggerLevel = ConfigurationManager.AppSettings["LoggerLevel"];
            if (!string.IsNullOrWhiteSpace(loggerLevel))
            {
                if (Enum.IsDefined(typeof(LogLevel), loggerLevel))
                {
                    request.LogLevel = (LogLevel)Enum.Parse(typeof(LogLevel), loggerLevel, true);
                }
            }

            string includeExternalRefs = ConfigurationManager.AppSettings["IncludeExternalReferences"];
            if (!string.IsNullOrWhiteSpace(includeExternalRefs))
            {
                if (bool.TryParse(includeExternalRefs, out bool result))
                {
                    request.IncludeExternalReferences = result;
                }
            }

            return request;
        }
    }
}
