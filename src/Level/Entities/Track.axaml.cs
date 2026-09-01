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

        public ushort unknown1;
        public byte flags;
        public byte x;
        public byte y;
        public TrackType type;
        public ushort lid;
        public ushort unknown2;
        public ushort unknown3;

        public Track() 
        {
            Width = 160;
            Height = 160;
            Content = new Avalonia.Controls.Shapes.Rectangle { Fill = Avalonia.Media.Brushes.DarkGoldenrod };
            PointerPressed += OnClick;
        }

        public override void LoadFromStream(KaitaiStream io)
        {
            unknown1 = io.ReadU2le();
            flags = io.ReadU1();
            x = io.ReadU1();
            y = io.ReadU1();
            type = (TrackType)io.ReadU1();
            lid = io.ReadU2le();
            unknown2 = io.ReadU2le();
            unknown3 = io.ReadU2le();

            UpdateSprite();
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(12);

            bb.Append(unknown1);
            bb.Append(flags);
            bb.Append(x);
            bb.Append(y);
            bb.Append((byte)type);
            bb.Append(lid);
            bb.Append(unknown2);
            bb.Append(unknown3);

            return bb.GetBytes();
        }

        public override void UpdateSprite()
        {
            Canvas.SetLeft(this, x * 160);
            Canvas.SetBottom(this, y * 160);
        }
    }
}
