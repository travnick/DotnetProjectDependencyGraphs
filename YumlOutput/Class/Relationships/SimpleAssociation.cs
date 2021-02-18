using System.Collections.Generic;
using YumlOutput.Class.Models;

namespace YumlOutput.Class.Relationships
{
    public sealed class SimpleAssociation : YumlRelationshipBase
    {
        public YumlModel Parent { get; }
        public YumlModel Child { get; }

        public SimpleAssociation(YumlModel parent, YumlModel child)
        {
            Parent = parent;
            Child = child;
        }

        public override ISet<string> GetDeclarations()
        {
            return new HashSet<string> { Parent.ToDeclarationString(), Child.ToDeclarationString() };
        }

        protected override string GenerateRelationMap()
        {
            return string.Format("{0}->{1}", Parent, Child);
        }

        protected override int GetHash()
        {
            return System.HashCode.Combine(Parent.ToString(), Child.ToString());
        }

        protected override bool EqualsImpl<T>(T other)
        {
            if (other is SimpleAssociation)
            {
                var o = other as SimpleAssociation;
                return o.Parent.Equals(Parent) && o.Child.Equals(Child);
            }

            return false;
        }
    }
}