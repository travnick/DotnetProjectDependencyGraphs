using System;
using System.IO;

namespace ProjectReferences.Models
{
    public sealed class InvestigationLink : IEquatable<InvestigationLink>
    {
        public InvestigationLink(ProjectLinkObject parent, ProjectLinkObject project)
        {
            Parent = parent;
            FullPath = project.FullPath;
            Guid = project.Id;
            IsOutOfSolution = project.IsOutOfSolution;
            IsProjectLoadable = File.Exists(FullPath);
        }

        public static InvestigationLink MakeOutOfSolution(ProjectLinkObject parent, string fullPath)
        {
            return new InvestigationLink { Parent = parent, FullPath = fullPath, IsOutOfSolution = true };
        }

        private InvestigationLink()
        {
        }

        public bool IsProjectLoadable { get; private set; }

        public ProjectLinkObject Parent { get; private set; }

        public string FullPath { get; private set; }

        public Guid Guid { get; private set; }

        public bool IsOutOfSolution { get; private set; }

        public bool Equals(InvestigationLink other)
        {
            return other.FullPath.Equals(FullPath);
        }
    }
}
