using System;

namespace YumlOutput.Class.Models
{
    public abstract class YumlModel : IComparable<YumlModel>
    {
        protected YumlModel(string name)
        {
            BackGroundColour = string.Empty;
            _name = name;
        }

        protected abstract string DrawForDeclaration();

        protected abstract string DrawForRelationship();

        protected abstract int Compare<T>(T other) where T : YumlModel;

        public string BackGroundColour { get; set; }

        public string GetName()
        {
            return _name;
        }

        public string ToDeclarationString()
        {
            string draw = DrawForDeclaration();

            if (!string.IsNullOrWhiteSpace(BackGroundColour) && !draw.Contains("{bg:"))
            {
                draw = draw.Substring(0, draw.Length - 1) + string.Format("{{bg:{0}}}]", BackGroundColour);
            }

            return draw;
        }

        public string ToRelationshipString()
        {
            return DrawForRelationship();
        }

        public override string ToString()
        {
            return ToRelationshipString();
        }

        public override bool Equals(object obj)
        {
            return CompareTo(obj as YumlModel) == 0;
        }

        public int CompareTo(YumlModel other)
        {
            return Compare(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_name);
        }

        private readonly string _name;
    }
}
