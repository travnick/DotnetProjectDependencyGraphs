using System.Collections.Generic;
using YumlOutput.Class.Models;

namespace YumlOutput.Class.Relationships
{
    public sealed class DirectionalAssociation : YumlRelationshipBase
    {
        public YumlModel Parent { get; }
        public YumlModel Child { get; }
        public string DirectionalMessage { get; }

        public override ISet<string> GetDeclarations()
        {
            return new HashSet<string> { Parent.ToDeclarationString(), Child.ToDeclarationString() };
        }

        protected override string GenerateRelationMap()
        {
            return string.Format("{0}-{1} >{2}", Parent, DirectionalMessage, Child);
        }

        protected override int GetHash()
        {
            return (Parent.ToString() + Child.ToString()).GetHashCode();
        }

        protected override bool EqualsImpl<T>(T other)
        {
            if (other is DirectionalAssociation)
            {
                var o = other as DirectionalAssociation;
                return o.Parent.Equals(Parent) && o.Child.Equals(Child);
            }

            return false;
        }
    }
}