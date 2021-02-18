using System.Collections.Generic;
using YumlOutput.Class.Models;

namespace YumlOutput.Class.Relationships
{
    public sealed class CardianRelationship : YumlRelationshipBase
    {
        public YumlModel Parent { get; }
        public string ParentCardianRelationShip { get; }

        public YumlModel Child { get; }
        public string ChildCardianRelationShip { get; }

        public override ISet<string> GetDeclarations()
        {
            return new HashSet<string> { Parent.ToDeclarationString(), Child.ToDeclarationString() };
        }

        protected override string GenerateRelationMap()
        {
            return string.Format("{0}{1}-{2}{3}", Parent, ParentCardianRelationShip, ChildCardianRelationShip, Child);
        }

        protected override int GetHash()
        {
            return System.HashCode.Combine(Parent.ToString(), Child.ToString());
        }

        protected override bool EqualsImpl<T>(T other)
        {
            if (other is CardianRelationship)
            {
                var o = other as CardianRelationship;
                return o.Parent.Equals(Parent) && o.Child.Equals(Child);
            }

            return false;
        }
    }
}