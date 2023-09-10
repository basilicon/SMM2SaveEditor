using Avalonia.Controls;
using Kaitai;
using SMM2SaveEditor;
using SMM2SaveEditor.Utility;
using System;

namespace SMM2SaveEditor.Entities.Nodes
{
    public partial class SnakeNode : Entity
    {
        public event EventHandler PostSpriteUpdate;

        ushort index;
        ushort direction;
        uint unknown1;

        public SnakeNode()
        {
            InitializeComponent();
        }

        public override void LoadFromStream(KaitaiStream io)
        {
            index = io.ReadU2le();
            direction = io.ReadU2le();
            unknown1 = io.ReadU4le();
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(8);

            bb.Append(index);
            bb.Append(direction);
            bb.Append(unknown1);

            return bb.GetBytes();
        }

        public override void UpdateSprite()
        {
            throw new System.NotImplementedException();
        }
    }
}