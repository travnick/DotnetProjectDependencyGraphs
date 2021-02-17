namespace YumlOutput.Class.Models
{
    public sealed class YumlNote : YumlModel
    {
        YumlNote(string name) : base(name)
        {
        }

        protected override string DrawForDeclaration()
        {
            return string.Format("[note: {0}]", GetName());
        }

        protected override string DrawForRelationship()
        {
            return DrawForDeclaration();
        }

        protected override int Compare<T>(T other)
        {
            if (!(other is YumlNote))
            {
                return -1;
            }

            var o = other as YumlNote;
            return this.GetName() == o.GetName() && BackGroundColour == o.BackGroundColour ? 0 : 1;
        }
    }
}