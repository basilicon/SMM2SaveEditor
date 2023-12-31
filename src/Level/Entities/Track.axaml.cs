using Kaitai;
using System.Collections;
using System.Collections.Generic;
using SMM2SaveEditor.Utility;
using Avalonia.Controls;
using System;

namespace SMM2SaveEditor.Entities
{
    public partial class Track : Entity
    {
        public event EventHandler PostSpriteUpdate;

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

        public override void LoadFromStream(KaitaiStream io)
        {
            unknown1 = io.ReadU2le();
            flags = io.ReadU1();
            x = io.ReadU1();
            y = io.ReadU1();
            type = io.ReadU1();
            lid = io.ReadU2le();
            unknown2 = io.ReadU2le();
            unknown3 = io.ReadU2le();

            Canvas.SetLeft(this, x * 160);
            Canvas.SetBottom(this, y * 160);
        }

        public override byte[] GetBytes()
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

        public override void UpdateSprite()
        {
            throw new System.NotImplementedException();
        }
    }
}
