using Kaitai;
using SMM2SaveEditor.Utility;
using SMM2SaveEditor.Entities.Nodes;
using System.Collections.Generic;
using Avalonia.Controls;
using System;

namespace SMM2SaveEditor.Entities
{
    public partial class PiranhaCreeper : Entity
    {
        public event EventHandler PostSpriteUpdate;

        byte unknown1;
        byte index;
        byte unknown2;
        List<PiranhaCreeperNode> nodes = new((int)Maxes.PiranhaCreeperNode);

        public PiranhaCreeper()
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
            ByteBuffer bb = new ByteBuffer(4 + (int)Maxes.PiranhaCreeperNode * 4);

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