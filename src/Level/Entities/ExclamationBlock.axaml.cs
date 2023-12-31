using Avalonia.Controls;
using Kaitai;
using SMM2SaveEditor.Entities.Nodes;
using SMM2SaveEditor.Utility;
using System;
using System.Collections.Generic;

namespace SMM2SaveEditor.Entities
{
    public partial class ExclamationBlock : Entity
    {
        public event EventHandler PostSpriteUpdate;

        byte unknown1;
        byte index;
        byte unknown2;
        List<ExclamationBlockNode> nodes = new((int)Maxes.ExclamationBlockNode);

        public ExclamationBlock() 
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
            ByteBuffer bb = new();

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

