using YumlOutput.Class.Models;

namespace YumlOutput.Class.Relationships
{
    public sealed class InterfaceInheritanceRelationship : YumlRelationshipBase
    {
        public YumlInterface Parent { get; }
        public YumlModel Child { get; }

        protected override string GenerateRelationMap()
        {
            return string.Format("{0}^-.-{1}", Parent, Child);
        }

        protected override int GetHash()
        {
            return (Parent.ToString() + Child.ToString()).GetHashCode();
        }

        protected override bool EqualsImpl<T>(T other)
        {
            if (other is InterfaceInheritanceRelationship)
            {
                var o = other as InterfaceInheritanceRelationship;
                return o.Parent.Equals(Parent) && o.Child.Equals(Child);
            }

            return false;
        }
    }
}