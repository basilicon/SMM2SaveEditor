using Kaitai;
using SMM2Level.Entities.Nodes;
using SMM2Level.Utility;
using System.Collections.Generic;

namespace SMM2Level.Entities
{
    public class TrackBlock : Entity
    {
        byte unknown1;
        byte index;
        byte numNodes;
        byte unknown2;
        List<TrackBlockNode> nodes = new((int)Maxes.TrackBlockNode);

        public override void LoadFromStream(KaitaiStream io)
        {
            unknown1 = io.ReadU1();
            index = io.ReadU1();
            numNodes = io.ReadU1();
            unknown2 = io.ReadU1();

            Level.FillLists(nodes, numNodes, (int)Maxes.TrackBlockNode, 
                "Prefabs/Entities/Nodes/TrackBlockNode", io, transform);
        }
    }
}
