using Avalonia.Controls;
using Kaitai;
using SMM2SaveEditor.Entities.Nodes;
using SMM2SaveEditor.Utility;
using System.Collections.Generic;
using System.Diagnostics;

namespace SMM2SaveEditor.Entities
{
    public partial class TrackBlock : UserControl, IEntity
    {
        public byte unknown1;
        public byte index;
        public byte unknown2;
        List<TrackBlockNode> nodes = new((int)Maxes.TrackBlockNode);

        public TrackBlock()
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
            ByteBuffer bb = new ByteBuffer(4 + (int)Maxes.TrackBlockNode * 4);

            bb.Append(unknown1);
            bb.Append(index);
            bb.Append((byte)nodes.Count);
            bb.Append(unknown2);

            bb.Append(LevelUtility.GetBytesFromList(nodes));

            return bb.GetBytes();
        }

        public void UpdateSprite()
        {
            throw new System.NotImplementedException();
        }
    }
}
