using System;
using System.Collections.Generic;
using System.IO;
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
        public ProjectDetail MakeProjectDetail(ProjectDetailRepository projectRepository, string fullFilePath, Guid guid, bool includeExternalReferences)
        {
            if (!_cachedProjects.ContainsKey(fullFilePath))
            {
                _cachedProjects.Add(fullFilePath, new ProjectFile(fullFilePath));
            }

            var projectFile = _cachedProjects[fullFilePath];

            return new ProjectDetail(projectRepository, fullFilePath, guid, projectFile, includeExternalReferences);
        }

        private readonly IDictionary<string, ProjectFile> _cachedProjects = new Dictionary<string, ProjectFile>();
    }
}
;
