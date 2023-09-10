using Avalonia.Controls;
using Kaitai;
using SMM2SaveEditor.Entities.Nodes;
using SMM2SaveEditor.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SMM2SaveEditor.Entities
{
    public partial class TrackBlock : Entity
    {
        public event EventHandler PostSpriteUpdate;

        public byte unknown1;
        public byte index;
        public byte unknown2;
        List<TrackBlockNode> nodes = new((int)Maxes.TrackBlockNode);

        public TrackBlock()
        {
            InitializeComponent();
        }

        public override void LoadFromStream(KaitaiStream io)
        {
            unknown1 = io.ReadU1();
            index = io.ReadU1();
            byte numNodes = io.ReadU1();
            unknown2 = io.ReadU1();

            LevelUtility.FillLists(ref nodes, numNodes, io);
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(4 + (int)Maxes.TrackBlockNode * 4);

            bb.Append(unknown1);
            bb.Append(index);
            bb.Append((byte)nodes.Count);
            bb.Append(unknown2);

            bb.Append(LevelUtility.GetBytesFromList(nodes));

            return bb.GetBytes();
        }

        public override void UpdateSprite()
        {
            throw new System.NotImplementedException();
        }
    }
}
