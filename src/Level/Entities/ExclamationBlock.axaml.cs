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

        public byte unknown1;
        public byte index;
        public byte unknown2;
        public List<ExclamationBlockNode> nodes = new((int)Maxes.ExclamationBlockNode);

        public ExclamationBlock() 
        {
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
            base.UpdateSprite();
        }
    }
}

