using YumlOutput.Class.Models;

namespace YumlOutput.Class.Relationships
{
    public sealed class CardianRelationship : YumlRelationshipBase
    {
        public YumlModel Parent { get; }
        public string ParentCardianRelationShip { get; }

        public YumlModel Child { get; }
        public string ChildCardianRelationShip { get; }

        protected override string GenerateRelationMap()
        {
            return string.Format("{0}{1}-{2}{3}", Parent, ParentCardianRelationShip, ChildCardianRelationShip, Child);
        }

        protected override int GetHash()
        {
            return (Parent.ToString() + Child.ToString()).GetHashCode();
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