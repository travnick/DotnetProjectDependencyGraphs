using System;

namespace ProjectReferences.Models
{
    public class ProjectLinkObject: IEquatable<ProjectLinkObject>
    {
        public ProjectLinkObject(ProjectDetail projectDetail)
        {
            FullPath = projectDetail.FullPath;
            Id = projectDetail.Id;
        }

        public ProjectLinkObject(string fullPath)
        {
            FullPath = fullPath;
        }

        public ProjectLinkObject(string fullPath, Guid id)
        {
            FullPath = fullPath;
            Id = id;
        }

        public string FullPath { get; private set; }
        public Guid Id { get; private set; }

        public bool Equals(ProjectLinkObject other)
        {
            return Id == other.Id;
        }
    }
}