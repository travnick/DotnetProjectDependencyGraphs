﻿using System.Collections.Generic;
using System.Text;
using YumlOutput.Class.Relationships;

namespace YumlOutput.Class
{
    public sealed class YumlClassDiagram
    {
        private readonly bool _newLineForEachRelationship;

        public YumlClassDiagram(bool newLineForEachRelationship = false)
        {
            Relationships = new HashSet<YumlRelationshipBase>();
            _newLineForEachRelationship = newLineForEachRelationship;
        }

        public ISet<YumlRelationshipBase> Relationships { get; private set;  }

        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var relationship in Relationships)
            {
                if (_newLineForEachRelationship)
                {
                    builder.AppendLine(relationship.ToString());
                }
                else
                {
                    builder.Append(relationship);
                }
            }

            return builder.ToString();
        }
    }
}