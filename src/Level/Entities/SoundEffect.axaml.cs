using Kaitai;
using System.Collections;
using System.Collections.Generic;
using SMM2Level.Utility;
using Avalonia.Controls;

namespace SMM2Level.Entities
{
    public partial class SoundEffect : Entity
    {
        byte id;
        byte x;
        byte y;
        byte unknown1;

        public override void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
        {
            id = io.ReadU1();
            x = io.ReadU1();
            y = io.ReadU1();
            unknown1 = io.ReadU1();

            Canvas.SetLeft(this, x);
            Canvas.SetBottom(this, y);

            // transform.position = new Vector2(x, y);
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(4);

            bb.Append(id);
            bb.Append(x);
            bb.Append(y);
            bb.Append(unknown1);

            return bb.GetBytes();
        }
    }
}
