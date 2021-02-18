using System;
using System.Collections.Generic;

namespace YumlOutput.Class.Relationships
{
    public abstract class YumlRelationshipBase : IEquatable<YumlRelationshipBase>
    {
        protected abstract string GenerateRelationMap();

        protected abstract int GetHash();

        protected abstract bool EqualsImpl<T>(T other) where T : YumlRelationshipBase;

        public abstract ISet<string> GetDeclarations();

        public bool Equals(YumlRelationshipBase other)
        {
            return EqualsImpl(other);
        }

        public override string ToString()
        {
            return GenerateRelationMap();
        }

        public override int GetHashCode()
        {
            return GetHash();
        }
    }
}
