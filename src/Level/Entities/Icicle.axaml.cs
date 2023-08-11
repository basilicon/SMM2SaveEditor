using Avalonia.Controls;
using Kaitai;
using SMM2Level.Utility;

namespace SMM2Level.Entities
{
    public partial class Icicle : Entity
    {
        byte x;
        byte y;
        byte type;
        byte unknown1;

        public override void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
        {
            x = io.ReadU1();
            y = io.ReadU1();
            type = io.ReadU1();
            unknown1 = io.ReadU1();

            Canvas.SetLeft(this, x);
            Canvas.SetBottom(this, y);
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(4);

            bb.Append(x);
            bb.Append(y);
            bb.Append(type);
            bb.Append(unknown1);

            return bb.GetBytes();
        }
    }
}