﻿using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YumlOutput.Class.Models
{
    public sealed class YumlClassWithDetails : YumlModel
    {
        public YumlClassWithDetails(string name)
        {
            Properties = new List<string>();
            Methods = new List<string>();
            Notes = new List<string>();

            Name = name;
        }

        public string Name { get; private set; }

        public IList<string> Properties { get; private set; }

        public IList<string> Methods { get; private set; }

        public IList<string> Notes { get; private set; }

        protected override string Draw()
        {
            var builder = new StringBuilder();
            builder.Append(string.Format("[{0}", Name));

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

        protected override int Compare<T>(T other)
        {
            if (!(other is YumlClassWithDetails))
            {
                return -1;
            }

            var o = other as YumlClassWithDetails;


            var basicCompare = Name == o.Name && BackGroundColour == o.BackGroundColour ? 0 : 1;
            if(basicCompare != 0)
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