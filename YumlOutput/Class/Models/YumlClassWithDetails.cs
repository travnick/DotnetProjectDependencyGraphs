using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YumlOutput.Class.Models
{
    public sealed class YumlClassWithDetails : YumlModel
    {
        public YumlClassWithDetails(string name) : base(name)
        {
            Properties = new List<string>();
            Methods = new List<string>();
            Notes = new List<string>();
        }

        public IList<string> Properties { get; private set; }

        public IList<string> Methods { get; private set; }

        public IList<string> Notes { get; private set; }

        protected override string DrawForDeclaration()
        {
            var builder = new StringBuilder();
            builder.Append(string.Format("[{0}", GetName()));

            if (Properties.Any())
            {
                builder.Append("|");

                foreach (var property in Properties)
                {
                    builder.Append(string.Format("+{0};", property));
                }
            }

            if (Methods.Any())
            {
                builder.Append("|");

                foreach (var method in Methods)
                {
                    builder.Append(string.Format("+{0}();", method));
                }
            }

            if (Notes.Any())
            {
                builder.Append("|");

                foreach (var note in Notes)
                {
                    builder.Append(string.Format("{0};", note));
                }
            }

            builder.Append("]");

            return builder.ToString();
        }

        protected override string DrawForRelationship()
        {
            return string.Format("[{0}]", GetName());
        }

        protected override int Compare<T>(T other)
        {
            if (!(other is YumlClassWithDetails))
            {
                return -1;
            }

            var o = other as YumlClassWithDetails;


            var basicCompare = GetName() == o.GetName() && BackGroundColour == o.BackGroundColour ? 0 : 1;
            if (basicCompare != 0)
            {
                return basicCompare;
            }

            if (Notes.Any(note => !o.Notes.Contains(note)))
            {
                return -1;
            }

            if (Methods.Any(method => !o.Methods.Contains(method)))
            {
                return -1;
            }

            if (Properties.Any(property => !o.Properties.Contains(property)))
            {
                return -1;
            }


            return basicCompare;
        }
    }
}