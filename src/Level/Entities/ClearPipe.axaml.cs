using Kaitai;
using SMM2SaveEditor.Utility;
using System.Collections.Generic;
using SMM2SaveEditor.Entities.Nodes;
using Avalonia.Controls;
using System;

namespace SMM2SaveEditor.Entities
{
    public partial class ClearPipe : Entity
    {
        public event EventHandler PostSpriteUpdate;

        public byte index;
        public byte numNodes;
        public ushort unknown;
        public List<ClearPipeNode> nodes = new((int)Maxes.ClearPipeNode);

        public ClearPipe() 
        {
        }

        public override void LoadFromStream(KaitaiStream io)
        {
            index = io.ReadU1();
            numNodes = io.ReadU1();
            unknown = io.ReadU2le();

            LevelUtility.FillLists(ref nodes, numNodes, io);
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer();

            bb.Append(index);
            bb.Append((byte)nodes.Count);
            bb.Append(unknown);

            bb.Append(LevelUtility.GetBytesFromList(nodes));

            return bb.GetBytes();
        }

        public override void UpdateSprite()
        {
            // Clear pipe visual rendering not yet implemented
        }
    }
}

