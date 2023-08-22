using Avalonia.Controls;
using Kaitai;
using SMM2SaveEditor.Utility;
using System.Diagnostics;

namespace SMM2SaveEditor.Entities.Nodes
{
    public partial class TrackBlockNode : UserControl, IEntity
    {
        public byte unknown1;
        public byte direction;
        public ushort unknown2;

        public TrackBlockNode()
        {
            InitializeComponent();
        }
 
        public void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
        {
            unknown1 = io.ReadU1();
            direction = io.ReadU1();
            unknown2 = io.ReadU2le();
        }

        public byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(4);

            bb.Append(unknown1);
            bb.Append(direction);
            bb.Append(unknown2);

            return bb.GetBytes();
        }

        public void UpdateSprite()
        {
            throw new System.NotImplementedException();
        }
    }
}
