using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Kaitai;
using SMM2SaveEditor.Utility;
using System;
using System.Collections.Generic;

namespace SMM2SaveEditor.Entities
{
    public partial class Icicle : Entity
    {
        public event EventHandler? PostSpriteUpdate;

        private static readonly Dictionary<string, Bitmap> bitmaps = new();
        private Image img;

        public byte x;
        public byte y;
        public IcicleType type;
        public byte unknown1;

        public Icicle()
        {
            Width = 160;
            Height = 320;
            ZIndex = 5;
            img = new Image { Stretch = Avalonia.Media.Stretch.Uniform };
            Avalonia.Media.RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
            Avalonia.Media.RenderOptions.SetBitmapInterpolationMode(img, BitmapInterpolationMode.None);
            Content = img;
            PointerPressed += OnClick;
        }

        public override void LoadFromStream(KaitaiStream io)
        {
            x = io.ReadU1();
            y = io.ReadU1();
            type = (IcicleType)io.ReadU1();
            unknown1 = io.ReadU1();

            UpdateSprite();
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(4);

            bb.Append(x);
            bb.Append(y);
            bb.Append((byte)type);
            bb.Append(unknown1);

            return bb.GetBytes();
        }

        public override void UpdateSprite()
        {
            Canvas.SetLeft(this, x * 160);
            Canvas.SetBottom(this, y * 160);
            Width = 160;
            Height = 320;

            string spriteName = type == IcicleType.fast_falling ? "118A" : "118";
            if (!bitmaps.TryGetValue(spriteName, out var bitmap))
            {
                bitmap = AssetHelper.LoadBitmap($"Assets/sprites/{spriteName}.png");
                if (bitmap != null) bitmaps[spriteName] = bitmap;
            }

            if (bitmap != null)
            {
                img.Source = bitmap;
            }
        }
    }
}