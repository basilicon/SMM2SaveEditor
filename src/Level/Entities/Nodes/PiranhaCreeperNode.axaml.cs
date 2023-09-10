using Avalonia.Controls;
using Kaitai;
using SMM2SaveEditor.Utility;
using System;

namespace SMM2SaveEditor.Entities.Nodes
{
    public partial class PiranhaCreeperNode : Entity
    {
        public event EventHandler PostSpriteUpdate;

        byte unknown1;
        byte direction;
        ushort unknown2;

        public PiranhaCreeperNode()
        {
            InitializeComponent();
        }

        public override void LoadFromStream(KaitaiStream io)
        {
            unknown1 = io.ReadU1();
            direction = io.ReadU1();
            unknown2 = io.ReadU2le();
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(4);

            bb.Append(unknown1);
            bb.Append(direction);
            bb.Append(unknown2);

            return bb.GetBytes();
        }

        public override void UpdateSprite()
        {
            throw new System.NotImplementedException();
        }
    }
}
