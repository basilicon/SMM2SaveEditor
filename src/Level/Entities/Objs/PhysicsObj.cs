

namespace SMM2SaveEditor.Entities.Objs
{
    // For objects which use SMM2's physics engine.

    public partial class PhysicsObj : Obj
    {
        public bool inPipe
        {
            get { return (flag & 0x1) != 0; }
            set { setFlagByMask(0x1, value); }
        }

        public bool winged
        { 
            get { return (flag & 0x2) != 0; }
            set { setFlagByMask(0x2, value); }
        }

        public bool inClowncar
        {
            get { return (flag & 0x200) != 0; }
            set { setFlagByMask(0x200, value); }
        }

        public bool tracked
        {
            get { return (flag & 0x400) != 0; }
            set { setFlagByMask(0x400, value); }
        }

        public bool inStack
        {
            get { return (flag & 0x1000) != 0; }
            set { setFlagByMask(0x1000, value); }
        }

        public bool parachute
        {
            get { return (flag & 0x8000) != 0; }
            set { setFlagByMask(0x8000, value); }
        }

        public bool inCloud
        {
            get { return (flag & 0x10000) != 0; }
            set { setFlagByMask(0x8000, value); }
        }

        public bool inClaw
        {
            get { return (flag & 0x8000000) != 0; }
            set { setFlagByMask(0x8000000, value); }
        }
    }
}
