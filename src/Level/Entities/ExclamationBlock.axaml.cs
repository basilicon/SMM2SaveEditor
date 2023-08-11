using Avalonia.Controls;
using Kaitai;
using SMM2Level.Entities.Nodes;
using SMM2Level.Utility;
using System.Collections.Generic;

namespace SMM2Level.Entities
{
    public partial class ExclamationBlock : Entity
    {
        byte unknown1;
        byte index;
        byte unknown2;
        List<ExclamationBlockNode> nodes = new((int)Maxes.ExclamationBlockNode);

        public override void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
        {
            unknown1 = io.ReadU1();
            index = io.ReadU1();
            byte numNodes = io.ReadU1();
            unknown2 = io.ReadU1();

            FillLists(nodes, numNodes, io);
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new();

            bb.Append(unknown1);
            bb.Append(index);
            bb.Append((byte)nodes.Count);
            bb.Append(unknown2);

            bb.Append(GetBytesFromList(nodes));

            return bb.GetBytes();
        }
    }
}

