using Kaitai;
using SMM2SaveEditor.Utility;
using System.Collections.Generic;
using SMM2SaveEditor.Entities.Nodes;
using Avalonia.Controls;
using System;

namespace SMM2SaveEditor.Entities
{
    public partial class Snake : Entity
    {
        public event EventHandler PostSpriteUpdate;

        byte index;
        ushort unknown1;
        List<SnakeNode> nodes = new(120);

        public Snake()
        {
            InitializeComponent();
        }

        public override void LoadFromStream(KaitaiStream io)
        {
            index = io.ReadU1();
            byte numNodes = io.ReadU1();
            unknown1 = io.ReadU2le();

            LevelUtility.FillLists(ref nodes, numNodes, io);
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(4 + (int)Maxes.SnakeNode * 8);

            bb.Append(index);
            bb.Append((byte)nodes.Count);
            bb.Append(unknown1);

            bb.Append(LevelUtility.GetBytesFromList(nodes));

            return bb.GetBytes();
        }

        public override void UpdateSprite()
        {
            throw new System.NotImplementedException();
        }
    }
}
