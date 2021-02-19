using System.Collections.Generic;

using YumlOutput.Class.Models;

namespace YumlOutput.Class.Relationships
{
    public sealed class CompositionRelationship : YumlRelationshipBase
    {
        public YumlModel Parent { get; }

        public YumlModel Child { get; }

        public int? CompositionCount { get; }

        public override ISet<string> GetDeclarations()
        {
            return new HashSet<string> { Parent.ToDeclarationString(), Child.ToDeclarationString() };
        }

        protected override string GenerateRelationMap()
        {
            return string.Format("{0}++-{1}{2}", Parent, CompositionCount.HasValue ? CompositionCount.Value.ToString() : string.Empty, Child);
        }

        protected override int GetHash()
        {
            return System.HashCode.Combine(Parent.ToString(), Child.ToString());
        }

        protected override bool EqualsImpl<T>(T other)
        {
            if (other is CompositionRelationship)
            {
                var o = other as CompositionRelationship;
                return o.Parent.Equals(Parent) && o.Child.Equals(Child);
            }

            return false;
        }
    }
}
