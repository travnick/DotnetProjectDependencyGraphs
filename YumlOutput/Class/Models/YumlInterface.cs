using System;

namespace YumlOutput.Class.Models
{
    public sealed class YumlInterface : YumlModel
    {
        YumlInterface(string name):base(name)
        {
        }

        protected override string DrawForDeclaration()
        {
            return string.Format("<<{0}>>", GetName());
        }

        protected override string DrawForRelationship()
        {
            return DrawForDeclaration();
        }

        protected override int Compare<T>(T other)
        {
            if (!(other is YumlInterface))
            {
                return -1;
            }

            var o = other as YumlInterface;
            return this.GetName() == o.GetName() && BackGroundColour == o.BackGroundColour ? 0 : 1;
        }
    }
}
