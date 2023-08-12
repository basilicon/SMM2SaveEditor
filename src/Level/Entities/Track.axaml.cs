using Kaitai;
using System.Collections;
using System.Collections.Generic;
using SMM2Level.Utility;
using Avalonia.Controls;

namespace SMM2Level.Entities
{
    public partial class Track : UserControl, IEntity
    {
        ushort unknown1;
        byte flags;
        byte x;
        byte y;
        byte type;
        ushort lid;
        ushort unknown2;
        ushort unknown3;

        public Track() 
        {
            InitializeComponent();
        }

        public void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
        {
            unknown1 = io.ReadU2le();
            flags = io.ReadU1();
            x = io.ReadU1();
            y = io.ReadU1();
            type = io.ReadU1();
            lid = io.ReadU2le();
            unknown2 = io.ReadU2le();
            unknown3 = io.ReadU2le();

            Canvas.SetLeft(this, x);
            Canvas.SetBottom(this, y);

            // transform.position = new Vector2(x, y);
        }

        public byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(12);

            bb.Append(unknown1);
            bb.Append(flags);
            bb.Append(x);
            bb.Append(y);
            bb.Append(type);
            bb.Append(lid);
            bb.Append(unknown2);
            bb.Append(unknown3);

            return bb.GetBytes();
        }
    }
}
