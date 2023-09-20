namespace SMM2SaveEditor.Entities.Objs.PhysicsObjs
{
    public partial class Enemy : PhysicsObj
    {
        public Sizes size
        {
            get
            {
                bool big = (flag & 0x4000) != 0,
                    medium = (flag & 0x2000) != 0;

                if (big) return Sizes.Big;
                if (medium) return Sizes.Medium;
                else return Sizes.Small;
            }
            set
            {
                setFlagByMask(0x4000, false);
                setFlagByMask(0x2000, false);
                if (value == Sizes.Big)
                {
                    setFlagByMask(0x4000, true);
                }
                else if (value == Sizes.Medium)
                {
                    setFlagByMask(0x2000, true);
                }
            }
        }
    }
}
