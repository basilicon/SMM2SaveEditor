using Kaitai;
using SMM2Level.Utility;
using System.Collections.Generic;
using SMM2Level.Entities.Nodes;

namespace SMM2Level.Entities
{
    public class Snake : Entity
    {
        byte index;
        byte numNodes;
        byte unknown1;
        List<SnakeNode> nodes = new(120);

        public override void LoadFromStream(KaitaiStream io)
        {
            index = io.ReadU1();
            numNodes = io.ReadU1();
            unknown1 = io.ReadU1();

            Level.FillLists(nodes, numNodes, (int)Maxes.SnakeNode, 
                "Prefabs/Entities/Nodes/SnakeNode", io, transform);
        }
    }
}
