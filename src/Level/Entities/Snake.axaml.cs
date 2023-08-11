using Kaitai;
using SMM2Level.Utility;
using System.Collections.Generic;
using SMM2Level.Entities.Nodes;
using Avalonia.Controls;

namespace SMM2Level.Entities
{
    public partial class Snake : Entity
    {
        byte index;
        ushort unknown1;
        List<SnakeNode> nodes = new(120);

        public override void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
        {
            index = io.ReadU1();
            byte numNodes = io.ReadU1();
            unknown1 = io.ReadU2le();

            FillLists(nodes, numNodes, io, canvas);
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(4 + (int)Maxes.SnakeNode * 8);

            bb.Append(index);
            bb.Append((byte)nodes.Count);
            bb.Append(unknown1);

            bb.Append(GetBytesFromList(nodes));

            return bb.GetBytes();
        }
    }
}
