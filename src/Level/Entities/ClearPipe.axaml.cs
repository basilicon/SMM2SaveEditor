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

        byte index;
        byte numNodes;
        ushort unknown;
        List<ClearPipeNode> nodes = new((int)Maxes.ClearPipeNode);

        public ClearPipe() 
        {
            InitializeComponent();
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
            bb.Append(numNodes);
            bb.Append(unknown);

            bb.Append(LevelUtility.GetBytesFromList(nodes));

            return bb.GetBytes();
        }

        public override void UpdateSprite()
        {
            throw new System.NotImplementedException();
        }
    }
}

