using System.Collections.Generic;

using YumlOutput.Class.Models;

namespace YumlOutput.Class.Relationships
{
    public sealed class NoteRelationship : YumlRelationshipBase
    {
        public YumlModel Item { get; }

        public YumlNote Note { get; }

        public override ISet<string> GetDeclarations()
        {
            return new HashSet<string> { Item.ToDeclarationString(), Note.ToDeclarationString() };
        }

        protected override string GenerateRelationMap()
        {
            return string.Format("{0}-{1}", Item, Note);
        }

        protected override int GetHash()
        {
            return System.HashCode.Combine(Item.ToString(), Note.ToString());
        }

        protected override bool EqualsImpl<T>(T other)
        {
            if (other is NoteRelationship)
            {
                var o = other as NoteRelationship;
                return o.Item.Equals(Item) && o.Note.Equals(Note);
            }

            return false;
        }
    }
}
