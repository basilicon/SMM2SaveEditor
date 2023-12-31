using Avalonia.Controls;
using Kaitai;
using SMM2SaveEditor.Utility;
using System;

namespace SMM2SaveEditor.Entities.Nodes
{
    public partial class ClearPipeNode : Entity
    {
        public event EventHandler PostSpriteUpdate;

        byte type;
        byte index;
        byte x;
        byte y;
        byte width;
        byte height;
        byte unknown1;
        byte direction;

        public ClearPipeNode()
        {
            InitializeComponent();
        }

        public override void LoadFromStream(KaitaiStream io)
        {
            type = io.ReadU1();
            index = io.ReadU1();
            x = io.ReadU1();
            y = io.ReadU1();
            width = io.ReadU1();
            height = io.ReadU1();
            unknown1 = io.ReadByte();
            direction = io.ReadByte();

            Canvas.SetLeft(this, x);
            Canvas.SetBottom(this, y);

            // transform.position = new Vector2(x, y);
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer();

            bb.Append(type);
            bb.Append(index);
            bb.Append(x);
            bb.Append(y);
            bb.Append(width);
            bb.Append(height);
            bb.Append(unknown1);
            bb.Append(direction);

            return bb.GetBytes();
        }

        public override void UpdateSprite()
        {
            throw new System.NotImplementedException();
        }
    }
}
