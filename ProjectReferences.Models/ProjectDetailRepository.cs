using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjectReferences.Models
{
    public sealed class ProjectDetailRepository : IEnumerable<ProjectDetail>
    {
        public void Add(ProjectDetail project)
        {
            var existingProject = GetById(project.Id);

            if (existingProject != null)
            {
                existingProject.AddParentLinks(project.ParentProjects);
                existingProject.AddChildLinks(project.ChildProjects);
            }
            else
            {
                _ = _projectDetails.Add(project);
            }
        }

        public ProjectDetail GetById(Guid id)
        {
            return _projectDetails.FirstOrDefault(p => p.Id.Equals(id));
        }

        public bool Has(Guid id)
        {
            return GetById(id) != null;
        }

        public IList<ProjectDetail> GetByName(string name)
        {
            return _projectDetails.Where(p => p.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)).ToList();
        }

        IEnumerator<ProjectDetail> IEnumerable<ProjectDetail>.GetEnumerator()
        {
            return _projectDetails.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _projectDetails.GetEnumerator();
        }

        public void Merge(ProjectDetailRepository other)
        {
            var projectsToAddToRepository = new HashSet<ProjectDetail>();
            var projectsWithPlaceholders = _projectDetails.Where(x => HasAnyPlaceholder(x));

            foreach (var projectWithPlaceholders in projectsWithPlaceholders)
            {
                projectsToAddToRepository.UnionWith(ReplaceChildProjectPlaceholders(other, projectWithPlaceholders.ChildProjects));
            }

            foreach (var projectToAdd in projectsToAddToRepository)
            {
                _ = _projectDetails.Add(projectToAdd);

                AddChildrenToRepository(other, projectToAdd.ChildProjects);
            }
        }

        private bool HasAnyPlaceholder(ProjectDetail x)
        {
            foreach (var childProject in x.ChildProjects)
            {
                if (childProject.IsOutOfSolution)
                {
                    return true;
                }
            }

            return false;
        }

        private void AddChildrenToRepository(ProjectDetailRepository other, ISet<ProjectLinkObject> childProjects)
        {
            foreach (var childProject in childProjects)
            {
                var project = other.GetById(childProject.Id);
                if (null != project)
                {
                    _ = _projectDetails.Add(project);

                    AddChildrenToRepository(other, project.ChildProjects);
                }
            }
        }

        private static ISet<ProjectDetail> ReplaceChildProjectPlaceholders(ProjectDetailRepository other, ISet<ProjectLinkObject> childProjects)
        {
            var projectsToRemove = new HashSet<ProjectLinkObject>();
            var projectLinksToAdd = new HashSet<ProjectLinkObject>();
            var projectsToAddToRepository = new HashSet<ProjectDetail>();

            foreach (var childProject in childProjects)
            {
                if (childProject.IsOutOfSolution)
                {
                    string name = Path.GetFileNameWithoutExtension(childProject.FullPath);

                    var possibleProjects = other.GetByName(name);

                    if (possibleProjects.Count == 0)
                    {
                    }
                    else if (possibleProjects.Count == 1)
                    {
                        _ = projectsToRemove.Add(childProject);
                        _ = projectsToAddToRepository.Add(possibleProjects.First());
                        _ = projectLinksToAdd.Add(new ProjectLinkObject(possibleProjects.First()));
                    }
                    else
                    {
                        throw new ArgumentException(string.Format("There is more than one possible project '{0}'", name));
                    }
                }
            }

            childProjects.ExceptWith(projectsToRemove);
            childProjects.UnionWith(projectLinksToAdd);

            return projectsToAddToRepository;
        }

        private readonly ISet<ProjectDetail> _projectDetails = new HashSet<ProjectDetail>();
    }
}
