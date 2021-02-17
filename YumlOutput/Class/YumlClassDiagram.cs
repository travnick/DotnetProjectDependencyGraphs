using System.Collections.Generic;
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

        public ISet<YumlRelationshipBase> Relationships { get; private set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            var declarations = new HashSet<string>();

            if (_newLineForEachRelationship)
            {
                builder.AppendLine();
                builder.AppendLine("// Relationships");
            }

            foreach (var relationship in Relationships)
            {
                declarations.UnionWith(relationship.GetDeclarations());

                if (_newLineForEachRelationship)
                {
                    builder.AppendLine(relationship.ToString());
                }
                else
                {
                    builder.Append(relationship);
                }
            }

            builder.Insert(0, GetDecarationsString(declarations));

            return builder.ToString();
        }

        private string GetDecarationsString(HashSet<string> declarations)
        {
            var builder = new StringBuilder();

            if (_newLineForEachRelationship)
            {
                builder.AppendLine("// Declarations");
            }

            foreach (var declaration in declarations)
            {
                if (_newLineForEachRelationship)
                {
                    builder.AppendLine(declaration);
                }
                else
                {
                    builder.Append(declaration);
                }
            }

            return builder.ToString();
        }
    }
}