using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectReferences.Models;

namespace ProjectReference
{
    public static class SolutionFileManager
    {
        /// <summary>
        /// Find projects that are referenced in a solution file and creates a list of references to them.
        /// </summary>
        /// <param name="rootNode"></param>
        /// <returns></returns>
        public static ISet<InvestigationLink> FindAllProjectLinks(RootNode rootNode)
        {
            var solution = new Solution(rootNode.File.FullName);

            var projects = solution.Projects.Where(p => p.RelativePath.EndsWith("proj")).ToList();
            var projectLinks = new HashSet<InvestigationLink>();
            projectLinks.UnionWith(
                from project in projects
                let path = Path.Combine(rootNode.Directory.FullName, project.RelativePath)
                select new InvestigationLink { Parent = null, FullPath = path }
            );

            return projectLinks;
        }
    }
}
