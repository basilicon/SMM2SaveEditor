using Kaitai;
using System.Collections;
using System.Collections.Generic;
using SMM2SaveEditor.Utility;
using Avalonia.Controls;
using System;

namespace SMM2SaveEditor.Entities
{
    public partial class SoundEffect : Entity
    {
        public event EventHandler PostSpriteUpdate;

        byte id;
        byte x;
        byte y;
        byte unknown1;

        public SoundEffect()
        {
            InitializeComponent();
        }

        public override void LoadFromStream(KaitaiStream io)
        {
            id = io.ReadU1();
            x = io.ReadU1();
            y = io.ReadU1();
            unknown1 = io.ReadU1();

            Canvas.SetLeft(this, x * 160);
            Canvas.SetBottom(this, y * 160);
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

        public override void UpdateSprite()
        {
            throw new System.NotImplementedException();
        }
    }
}
