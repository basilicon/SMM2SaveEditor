using Avalonia.Controls;
using Kaitai;
using SMM2Level.Utility;

namespace SMM2Level.Entities.Nodes
{
    public partial class ExclamationBlockNode : UserControl, IEntity
    {
        byte unknown1;
        byte direction;
        ushort unknown2;

        public ExclamationBlockNode()
        {
            InitializeComponent();
        }

        public void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
        {
            unknown1 = io.ReadU1();
            direction = io.ReadU1();
            unknown2 = io.ReadU2le();
        }

        public   byte[] GetBytes()
        {
            ByteBuffer bb = new(4);

            bb.Append(unknown1);
            bb.Append(direction);
            bb.Append(unknown2);

            return bb.GetBytes();
        }
    }
}
