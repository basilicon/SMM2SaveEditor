using Kaitai;
using SMM2Level.Utility;
using System.Collections.Generic;
using SMM2Level.Entities.Nodes;
using Avalonia.Controls;

namespace SMM2Level.Entities
{
    public partial class ClearPipe : Entity
    {
        byte index;
        byte numNodes;
        ushort unknown;
        List<ClearPipeNode> nodes = new((int)Maxes.ClearPipeNode);

        public override void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
        {
            index = io.ReadU1();
            numNodes = io.ReadU1();
            unknown = io.ReadU2le();

            Entity.FillLists(nodes, numNodes, io);
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer();

            bb.Append(index);
            bb.Append(numNodes);
            bb.Append(unknown);

            bb.Append(GetBytesFromList(nodes));

            return bb.GetBytes();
        }
    }
}

