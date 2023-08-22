using Avalonia.Controls;
using Kaitai;
using SMM2SaveEditor.Utility;

namespace SMM2SaveEditor.Entities
{
    public partial class Icicle : UserControl, IEntity
    {
        byte x;
        byte y;
        byte type;
        byte unknown1;

        public Icicle()
        {
            InitializeComponent();
        }

        public void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
        {
            x = io.ReadU1();
            y = io.ReadU1();
            type = io.ReadU1();
            unknown1 = io.ReadU1();

            Canvas.SetLeft(this, x);
            Canvas.SetBottom(this, y);
        }

        public byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(4);

            bb.Append(x);
            bb.Append(y);
            bb.Append(type);
            bb.Append(unknown1);

            return bb.GetBytes();
        }
    }
}