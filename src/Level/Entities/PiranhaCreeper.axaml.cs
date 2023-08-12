using Kaitai;
using SMM2Level.Utility;
using SMM2Level.Entities.Nodes;
using System.Collections.Generic;
using Avalonia.Controls;

namespace SMM2Level.Entities
{
    public partial class PiranhaCreeper : UserControl, IEntity
    {
        byte unknown1;
        byte index;
        byte unknown2;
        List<PiranhaCreeperNode> nodes = new((int)Maxes.PiranhaCreeperNode);

        public PiranhaCreeper()
        {
            InitializeComponent();
        }

        public void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
        {
            unknown1 = io.ReadU1();
            index = io.ReadU1();
            byte numNodes = io.ReadU1();
            unknown2 = io.ReadU1();

            LevelUtility.FillLists(nodes, numNodes, io, canvas);
        }

        public byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(4 + (int)Maxes.PiranhaCreeperNode * 4);

            bb.Append(unknown1);
            bb.Append(index); 
            bb.Append((byte)nodes.Count);
            bb.Append(unknown2);

            bb.Append(LevelUtility.GetBytesFromList(nodes));

            return bb.GetBytes();
        }
    }
}