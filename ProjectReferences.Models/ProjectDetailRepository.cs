﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ProjectReferences.Models
{
    public sealed class ProjectDetailRepository: IEnumerable<ProjectDetail>
    {
        private readonly IList<ProjectDetail> _projectDetails = new List<ProjectDetail>();

        public void Add(ProjectDetail project)
        {
            var proj = _projectDetails.FirstOrDefault(p => p.Id.Equals(project.Id));

            if (proj != null)
            {
                proj.AddParentLinks(project.ParentProjects);
            }
            else
            {
                _projectDetails.Add(project);
            }
        }

        public ProjectDetail GetById(Guid id)
        {
            return _projectDetails.FirstOrDefault(p => p.Id.Equals(id));
        }

        public IList<ProjectDetail> GetByName(string name)
        {
            return _projectDetails.Where(p => p.Name.Equals(name)).ToList();
        }

        IEnumerator<ProjectDetail> IEnumerable<ProjectDetail>.GetEnumerator()
        {
            return _projectDetails.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _projectDetails.GetEnumerator();
        }
    }
}