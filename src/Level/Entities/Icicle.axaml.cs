using Avalonia.Controls;
using Kaitai;
using SMM2SaveEditor.Utility;
using System;

namespace SMM2SaveEditor.Entities
{
    public partial class Icicle : Entity
    {
        public event EventHandler PostSpriteUpdate;

        public byte x;
        public byte y;
        public IcicleType type;
        public byte unknown1;

        public Icicle()
        {
            Width = 160;
            Height = 160;
            Content = new Avalonia.Controls.Shapes.Rectangle { Fill = Avalonia.Media.Brushes.LightCyan };
            PointerPressed += OnClick;
        }

        public override void LoadFromStream(KaitaiStream io)
        {
            x = io.ReadU1();
            y = io.ReadU1();
            type = (IcicleType)io.ReadU1();
            unknown1 = io.ReadU1();

            UpdateSprite();
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(4);

            bb.Append(x);
            bb.Append(y);
            bb.Append((byte)type);
            bb.Append(unknown1);

            return bb.GetBytes();
        }

        public override void UpdateSprite()
        {
            Canvas.SetLeft(this, x * 160);
            Canvas.SetBottom(this, y * 160);
        }
    }
}