using Kaitai;
using SMM2Level.Utility;
using SMM2Level.Entities.Nodes;
using System.Collections.Generic;

namespace SMM2Level.Entities
{
    public class PiranhaCreeper : Entity
    {
        byte unknown1;
        byte index;
        byte numNodes;
        byte unknown2;
        List<PiranhaCreeperNode> nodes = new((int)Maxes.PiranhaCreeperNode);

        public override void LoadFromStream(KaitaiStream io)
        {
            unknown1 = io.ReadU1();
            index = io.ReadU1();
            numNodes = io.ReadU1();
            unknown2 = io.ReadU1();

            Level.FillLists(nodes, numNodes, (int)Maxes.PiranhaCreeperNode, 
                "Prefabs/Entities/Nodes/PiranhaCreeperNode", io, transform);
        }

        public byte[] ExportBytes()
        {
            return new byte[1];
        }
    }
}