using Kaitai;
using System.Collections.Generic;
using SMM2SaveEditor.Utility;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using Avalonia;

namespace SMM2SaveEditor.Entities
{
    public partial class Track : Entity
    {
        public event EventHandler? PostSpriteUpdate;

        private static readonly Dictionary<string, Bitmap> bitmaps = new();
        private Image img;
        private Canvas capCanvas;
        private Grid rootGrid;

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

            img = new Image { Stretch = Stretch.Fill };
            Avalonia.Media.RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
            Avalonia.Media.RenderOptions.SetBitmapInterpolationMode(img, BitmapInterpolationMode.None);

            capCanvas = new Canvas();

            rootGrid = new Grid();
            rootGrid.Children.Add(img);
            rootGrid.Children.Add(capCanvas);

            Content = rootGrid;
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
            int typeId = (int)type;
            bool isLarge = typeId >= 8;
            int tileSpan = isLarge ? 5 : 3;
            int size = tileSpan * 160;

            Canvas.SetLeft(this, x * 160);
            Canvas.SetBottom(this, y * 160);
            Width = size;
            Height = size;
            img.Width = size;
            img.Height = size;
            capCanvas.Width = size;
            capCanvas.Height = size;

            string spriteName = $"T{typeId}";
            if (!bitmaps.TryGetValue(spriteName, out var bitmap))
            {
                bitmap = AssetHelper.LoadBitmap($"Assets/sprites/{spriteName}.png");
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

            // Update Capping Stopper Visualization
            capCanvas.Children.Clear();

            if (typeId >= 0 && typeId < TrackPorts.Length)
            {
                var (defaultP1, defaultP2, defaultP3) = TrackPorts[typeId];

                byte u2_lo = (byte)(unknown2 & 0xFF);
                byte u2_hi = (byte)(unknown2 >> 8);
                byte u3_lo = (byte)(unknown3 & 0xFF);
                byte u3_hi = (byte)(unknown3 >> 8);

                // Use custom socket from byte if present, otherwise default topology port
                int socket1 = (u2_lo & 0x0F) < 8 ? (u2_lo & 0x0F) : defaultP1;
                int socket2 = (u3_lo & 0x0F) < 8 ? (u3_lo & 0x0F) : defaultP2;

                if (defaultP3 == null)
                {
                    // 2-Port Standard & Curved Tracks
                    if ((u2_lo & 0xF0) == 0x70) AddCapAtPort(socket1, tileSpan);
                    if ((u3_lo & 0xF0) == 0x70) AddCapAtPort(socket2, tileSpan);
                }
                else
                {
                    // 3-Port Y-Shaped Junction Tracks (p1 = Stem, p2 = Fork 1, p3 = Fork 2)
                    bool stemCapped = (u2_lo & 0xF0) == 0x70;
                    bool fork1Capped = typeId >= 12 ? (u2_hi & 0x10) == 0 : (u2_hi & 0x80) != 0;
                    bool fork2Capped = typeId >= 12 ? (u3_hi & 0x01) != 0 : (u3_lo & 0x40) == 0;

                    if (stemCapped) AddCapAtPort(socket1, tileSpan);
                    if (fork1Capped) AddCapAtPort(defaultP2, tileSpan);
                    if (fork2Capped) AddCapAtPort(defaultP3.Value, tileSpan);
                }
            }
        }

        private static readonly (int port1, int port2, int? port3)[] TrackPorts = new[]
        {
            /* 0  horizontal        */ (1, 0, (int?)null),
            /* 1  vertical          */ (2, 3, (int?)null),
            /* 2  slope_up_right    */ (5, 4, (int?)null),
            /* 3  slope_down_right  */ (6, 7, (int?)null),
            /* 4  curve_upper_left  */ (5, 4, (int?)null),
            /* 5  curve_upper_right */ (5, 4, (int?)null),
            /* 6  curve_lower_right */ (6, 7, (int?)null),
            /* 7  curve_lower_left  */ (6, 7, (int?)null),
            /* 8  y_shape_up_left   */ (1, 4, (int?)7),
            /* 9  y_shape_up_right  */ (0, 6, (int?)5),
            /* 10 y_shape_down_left */ (3, 5, (int?)7),
            /* 11 y_shape_down_right*/ (2, 6, (int?)4),
            /* 12 y_shape_right_down*/ (1, 4, (int?)7),
            /* 13 y_shape_left_down */ (0, 6, (int?)5),
            /* 14 y_shape_right_up  */ (3, 5, (int?)7),
            /* 15 y_shape_left_up   */ (2, 6, (int?)4),
        };

        private static Bitmap? capBitmap;

        private void AddCapAtPort(int port, int tileSpan)
        {
            (double cx, double cy) = GetPortCenter(port, tileSpan);
            Control cap = CreateStopperCap();
            Canvas.SetLeft(cap, cx - 80);
            Canvas.SetTop(cap, cy - 80);
            capCanvas.Children.Add(cap);
        }

        private static (double cx, double cy) GetPortCenter(int port, int tileSpan)
        {
            int tx = port switch
            {
                1 or 5 or 6 => 0,
                0 or 4 or 7 => tileSpan - 1,
                _ => tileSpan / 2
            };

            int ty = port switch
            {
                2 or 5 or 7 => 0,
                3 or 4 or 6 => tileSpan - 1,
                _ => tileSpan / 2
            };

            return (tx * 160 + 80, ty * 160 + 80);
        }

        private static Control CreateStopperCap()
        {
            if (capBitmap == null)
            {
                capBitmap = AssetHelper.LoadBitmap("Assets/sprites/T.png");
            }

            var img = new Image
            {
                Width = 160,
                Height = 160,
                Source = capBitmap,
                Stretch = Stretch.Uniform
            };
            Avalonia.Media.RenderOptions.SetBitmapInterpolationMode(img, BitmapInterpolationMode.None);

            return img;
        }
    }
}
