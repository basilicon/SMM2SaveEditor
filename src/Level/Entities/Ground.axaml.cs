using Kaitai;
using SMM2SaveEditor.Utility;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System;

namespace SMM2SaveEditor.Entities
{
    public partial class Ground : Entity
    {
        public event EventHandler PostSpriteUpdate;

        public byte x;
        public byte y;
        public byte id;
        public byte backgroundId;

        private Image sprite;
        private static Bitmap? bitmap = null;

        public Ground()
        {
            InitializeComponent();
            sprite = this.Find<Image>("Sprite");

            if (bitmap == null)
            {
                bitmap = new Bitmap(
#if RELEASE
                "./Assets/sprites/7.png"
#else
                "../../../Assets/sprites/7.png"
#endif
                );
            }
        }

        public override void LoadFromStream(KaitaiStream io)
        {
            x = io.ReadU1();
            y = io.ReadU1();
            id = io.ReadU1();
            backgroundId = io.ReadU1();

            UpdateSprite();
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(4);

            bb.Append(x);
            bb.Append(y);
            bb.Append(id);
            bb.Append(backgroundId);

            return bb.GetBytes();
        }

        public override void UpdateSprite()
        {
            // throw new System.NotImplementedException();
            Canvas.SetLeft(this, x * 160);
            Canvas.SetBottom(this, y * 160);

            sprite.Source = bitmap;
            sprite.PointerPressed += OnClick;
        }
    }
}