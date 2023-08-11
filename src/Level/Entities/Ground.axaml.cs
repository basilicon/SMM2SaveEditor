using Kaitai;
using System.Collections;
using System.Collections.Generic;
using SMM2Level.Utility;
using Avalonia.Controls;

namespace SMM2Level.Entities
{
    public partial class Ground : Entity
    {
        public byte x;
        public byte y;
        public byte id;
        public byte backgroundId;

        public override void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
        {
            x = io.ReadU1();
            y = io.ReadU1();
            id = io.ReadU1();
            backgroundId = io.ReadU1();

            //transform.position = new Vector2(x, y);
            Canvas.SetLeft(this, x);
            Canvas.SetBottom(this, y);
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(4);

            bb.Append(x);
            bb.Append(y);
            bb.Append(id);
            bb.Append(backgroundId);

            return bb.GetBytes();
        }
    }
}