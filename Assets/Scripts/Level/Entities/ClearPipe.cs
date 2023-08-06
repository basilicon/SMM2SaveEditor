using Kaitai;
using SMM2Level.Utility;
using System.Collections.Generic;
using SMM2Level.Entities.Nodes;

namespace SMM2Level.Entities
{
    public class ClearPipe : Entity
    {
        byte index;
        byte numNodes;
        ushort unknown;
        List<ClearPipeNode> nodes = new((int)Maxes.ClearPipeNode);

        public override void LoadFromStream(KaitaiStream io)
        {
            index = io.ReadU1();
            numNodes = io.ReadU1();
            unknown = io.ReadU2le();

            Level.FillLists(nodes, numNodes, (int)Maxes.ClearPipeNode, 
                "Prefabs/Entities/Nodes/ClearPipeNode", io, transform);
        }
    }
}

