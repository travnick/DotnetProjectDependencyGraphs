using System.Collections.Generic;
using YumlOutput.Class.Models;

namespace YumlOutput.Class.Relationships
{
    public sealed class AggregationRelationship : YumlRelationshipBase
    {
        public YumlModel Parent { get; }

        public YumlModel Child { get; }

        public int? AggregateCount { get; }

        public override ISet<string> GetDeclarations()
        {
            return new HashSet<string> { Parent.ToDeclarationString(), Child.ToDeclarationString() };
        }

        protected override string GenerateRelationMap()
        {
            if (AggregateCount.HasValue)
            {
                return string.Format("{0}<>-{1}{2}", Parent, AggregateCount.Value, Child);
            }

            return string.Format("{0}+->{1}", Parent, Child);
        }

        protected override int GetHash()
        {
            return System.HashCode.Combine(Parent.ToString(), Child.ToString());
        }

        protected override bool EqualsImpl<T>(T other)
        {
            if (other is AggregationRelationship)
            {
                var o = other as AggregationRelationship;
                return o.Parent.Equals(Parent) && o.Child.Equals(Child);
            }

            return false;
        }
    }
}
