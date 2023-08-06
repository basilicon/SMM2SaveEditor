using Kaitai;
using SMM2Level.Entities.Nodes;
using SMM2Level.Utility;
using System.Collections.Generic;

namespace SMM2Level.Entities
{
    public class ExclamationBlock : Entity
    {
        byte unknown1;
        byte index;
        byte numNodes;
        byte unknown2;
        List<ExclamationBlockNode> nodes = new((int)Maxes.ExclamationBlockNode);

        public override void LoadFromStream(KaitaiStream io)
        {
            unknown1 = io.ReadU1();
            index = io.ReadU1();
            numNodes = io.ReadU1();
            unknown2 = io.ReadU1();

            Level.FillLists(nodes, numNodes, (int)Maxes.ExclamationBlockNode, 
                "Prefabs/Entities/Nodes/ExclamationBlockNode", io, transform);
        }
    }
}

