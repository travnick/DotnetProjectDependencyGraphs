using System;

namespace ProjectReferences.Models
{
    public sealed class InvestigationLink : IEquatable<InvestigationLink>
    {
        public ProjectLinkObject Parent { get; set; }

        public string FullPath { get; set; }

        public bool Equals(InvestigationLink other)
        {
            return other.FullPath.Equals(FullPath);
        }
    }
}