using Avalonia.Controls;
using Kaitai;
using SMM2SaveEditor.Utility;
using System;

namespace SMM2SaveEditor.Entities
{
    public partial class Icicle : Entity
    {
        public event EventHandler PostSpriteUpdate;

        byte x;
        byte y;
        byte type;
        byte unknown1;

        public Icicle()
        {
            InitializeComponent();
        }

        public override void LoadFromStream(KaitaiStream io)
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

        public override void UpdateSprite()
        {
            throw new System.NotImplementedException();
        }
    }
}