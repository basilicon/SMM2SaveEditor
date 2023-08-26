using Kaitai;
using SMM2SaveEditor.Utility;
using System.Collections.Generic;
using SMM2SaveEditor.Entities.Nodes;
using Avalonia.Controls;

namespace SMM2SaveEditor.Entities
{
    public partial class Snake : UserControl, IEntity
    {
        byte index;
        ushort unknown1;
        List<SnakeNode> nodes = new(120);

        public Snake()
        {
            InitializeComponent();
        }

        public void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
        {
            index = io.ReadU1();
            byte numNodes = io.ReadU1();
            unknown1 = io.ReadU2le();

            LevelUtility.FillLists(ref nodes, numNodes, io, canvas);
        }

        public byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(4 + (int)Maxes.SnakeNode * 8);

            bb.Append(index);
            bb.Append((byte)nodes.Count);
            bb.Append(unknown1);

            bb.Append(LevelUtility.GetBytesFromList(nodes));

            return bb.GetBytes();
        }

        public void UpdateSprite()
        {
            throw new System.NotImplementedException();
        }
    }
}
