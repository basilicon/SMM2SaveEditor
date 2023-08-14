using Kaitai;
using SMM2Level.Utility;
using Avalonia.Controls;
using System.Diagnostics;
using Avalonia.Media.Imaging;
using System.Xml.Linq;

namespace SMM2Level.Entities
{
    public partial class Ground : UserControl, IEntity
    {
        public byte x;
        public byte y;
        public byte id;
        public byte backgroundId;

        private Image sprite;

        public Ground()
        {
            InitializeComponent();
            sprite = this.Find<Image>("Sprite");
        }

        public void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
        {
            x = io.ReadU1();
            y = io.ReadU1();
            id = io.ReadU1();
            backgroundId = io.ReadU1();

            Canvas.SetLeft(this, x * 160);
            Canvas.SetBottom(this, y * 160);

            sprite.Source = new Bitmap("../../../img/sprites/7.png");
        }

        public byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(4);

            bb.Append((byte)(x / 160));
            bb.Append((byte)(y / 160));
            bb.Append(id);
            bb.Append(backgroundId);

            return bb.GetBytes();
        }
    }
}