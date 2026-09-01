using Kaitai;
using System.Collections.Generic;
using SMM2SaveEditor.Utility;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System;

namespace SMM2SaveEditor.Entities
{
    public partial class Track : Entity
    {
        public event EventHandler? PostSpriteUpdate;

        private static readonly Dictionary<string, Bitmap> bitmaps = new();
        private Image img;

        public ushort unknown1;
        public byte flags;
        public byte x;
        public byte y;
        public TrackType type;
        public ushort lid;
        public ushort unknown2;
        public ushort unknown3;

        public Track() 
        {
            Width = 480;
            Height = 480;
            ZIndex = 2;
            img = new Image { Stretch = Avalonia.Media.Stretch.Uniform };
            Avalonia.Media.RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
            Avalonia.Media.RenderOptions.SetBitmapInterpolationMode(img, BitmapInterpolationMode.None);
            Content = img;
            PointerPressed += OnClick;
        }

        public override void LoadFromStream(KaitaiStream io)
        {
            unknown1 = io.ReadU2le();
            flags = io.ReadU1();
            x = io.ReadU1();
            y = io.ReadU1();
            type = (TrackType)io.ReadU1();
            lid = io.ReadU2le();
            unknown2 = io.ReadU2le();
            unknown3 = io.ReadU2le();

            UpdateSprite();
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(12);

            bb.Append(unknown1);
            bb.Append(flags);
            bb.Append(x);
            bb.Append(y);
            bb.Append((byte)type);
            bb.Append(lid);
            bb.Append(unknown2);
            bb.Append(unknown3);

            return bb.GetBytes();
        }

        public override void UpdateSprite()
        {
            bool isLarge = (flags & 0x1) != 0;
            int tileSpan = isLarge ? 5 : 3;
            int offset = isLarge ? 2 : 1;
            int size = tileSpan * 160;

            Canvas.SetLeft(this, (x - offset) * 160);
            Canvas.SetBottom(this, (y - offset) * 160);
            Width = size;
            Height = size;

            int spriteIndex = (int)type;
            if (isLarge && spriteIndex < 8)
            {
                spriteIndex += 8;
            }

            string spriteName = $"T{spriteIndex}";
            if (!bitmaps.TryGetValue(spriteName, out var bitmap))
            {
                bitmap = AssetHelper.LoadBitmap($"Assets/sprites/{spriteName}.png");
                if (bitmap == null && isLarge)
                {
                    bitmap = AssetHelper.LoadBitmap($"Assets/sprites/T{(int)type}.png");
                }
                if (bitmap == null)
                {
                    bitmap = AssetHelper.LoadBitmap("Assets/sprites/T.png");
                }
                if (bitmap != null) bitmaps[spriteName] = bitmap;
            }

            if (bitmap != null)
            {
                img.Source = bitmap;
            }
        }
    }
}
