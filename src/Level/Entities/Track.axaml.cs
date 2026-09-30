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
        private static readonly Dictionary<string, Bitmap> bitmaps = new();
        private static readonly Dictionary<int, Bitmap> centerBitmaps = new();
        private static readonly Dictionary<TrackSocket, Bitmap> edgeBitmaps = new();

        private Image img;
        private Canvas trackCanvas;
        private Image centerImg;
        private Image ep1Img;
        private Image ep1DirectedImg;
        private Image ep2Img;
        private Image ep2DirectedImg;
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

            trackCanvas = new Canvas();
            centerImg = new Image { Width = 160, Height = 160, Stretch = Stretch.Fill };
            ep1Img = new Image { Width = 160, Height = 160, Stretch = Stretch.Fill };
            ep1DirectedImg = new Image { Width = 160, Height = 160, Stretch = Stretch.Fill };
            ep2Img = new Image { Width = 160, Height = 160, Stretch = Stretch.Fill };
            ep2DirectedImg = new Image { Width = 160, Height = 160, Stretch = Stretch.Fill };

            Avalonia.Media.RenderOptions.SetBitmapInterpolationMode(centerImg, BitmapInterpolationMode.None);
            Avalonia.Media.RenderOptions.SetBitmapInterpolationMode(ep1Img, BitmapInterpolationMode.None);
            Avalonia.Media.RenderOptions.SetBitmapInterpolationMode(ep1DirectedImg, BitmapInterpolationMode.None);
            Avalonia.Media.RenderOptions.SetBitmapInterpolationMode(ep2Img, BitmapInterpolationMode.None);
            Avalonia.Media.RenderOptions.SetBitmapInterpolationMode(ep2DirectedImg, BitmapInterpolationMode.None);

            trackCanvas.Children.Add(centerImg);
            trackCanvas.Children.Add(ep1Img);
            trackCanvas.Children.Add(ep1DirectedImg);
            trackCanvas.Children.Add(ep2Img);
            trackCanvas.Children.Add(ep2DirectedImg);

            capCanvas = new Canvas();

            rootGrid = new Grid();
            rootGrid.Children.Add(img);
            rootGrid.Children.Add(trackCanvas);
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

        public override bool TryMoveBy(int deltaTilesX, int deltaTilesY)
        {
            int newX = Math.Clamp(x + deltaTilesX, 0, 240);
            int newY = Math.Clamp(y + deltaTilesY, 0, 27);
            if (newX != x || newY != y)
            {
                x = (byte)newX;
                y = (byte)newY;
                UpdateSprite();
                return true;
            }
            return false;
        }

        public override void UpdateSprite()
        {
            int typeId = (int)type;
            bool isLarge = typeId >= 8;
            int tileSpan = isLarge ? 5 : 3;
            int size = tileSpan * 160;
            int offset = tileSpan / 2;

            // Center track node on block (x, y)
            Canvas.SetLeft(this, (x - offset) * 160);
            Canvas.SetBottom(this, (y - offset) * 160);
            Width = size;
            Height = size;
            img.Width = size;
            img.Height = size;
            trackCanvas.Width = size;
            trackCanvas.Height = size;
            capCanvas.Width = size;
            capCanvas.Height = size;

            // Check cut flags (0x0104): endpoints do not spawn outside block (x, y)
            bool isP1Cut = unknown2 == 0x0104;
            bool isP2Cut = unknown3 == 0x0104;

            capCanvas.Children.Clear();

            byte u2_lo = (byte)(unknown2 & 0xFF);
            byte u2_hi = (byte)(unknown2 >> 8);
            byte u3_lo = (byte)(unknown3 & 0xFF);
            byte u3_hi = (byte)(unknown3 >> 8);

            if (typeId >= 0 && typeId <= 3)
            {
                // Modular rendering for straight tracks (Types 0..3)
                img.IsVisible = false;
                img.Clip = null;
                trackCanvas.IsVisible = true;

                centerImg.Source = GetCenterBitmap(typeId);
                Canvas.SetLeft(centerImg, offset * 160);
                Canvas.SetTop(centerImg, offset * 160);

                var (defaultPort1, defaultPort2, _) = TrackPorts[typeId];
                TrackSocket sock1 = ResolveEndpointSocket(unknown2, defaultPort1);
                TrackSocket sock2 = ResolveEndpointSocket(unknown3, defaultPort2);

                // Endpoint 1 (in defaultPort1 box)
                (double cx1, double cy1) = GetPortCenter(defaultPort1, tileSpan);
                if (isP1Cut)
                {
                    ep1Img.IsVisible = false;
                    ep1DirectedImg.IsVisible = false;
                }
                else
                {
                    ep1Img.IsVisible = true;
                    ep1Img.Source = GetEdgeBitmap(defaultPort1);
                    Canvas.SetLeft(ep1Img, cx1 - 80);
                    Canvas.SetTop(ep1Img, cy1 - 80);

                    if (IsConnectedTransition(unknown2))
                    {
                        ep1DirectedImg.IsVisible = true;
                        ep1DirectedImg.Source = GetRayBitmap(sock1);
                        Canvas.SetLeft(ep1DirectedImg, cx1 - 80);
                        Canvas.SetTop(ep1DirectedImg, cy1 - 80);
                    }
                    else
                    {
                        ep1DirectedImg.IsVisible = false;
                    }
                }

                // Endpoint 2 (in defaultPort2 box)
                (double cx2, double cy2) = GetPortCenter(defaultPort2, tileSpan);
                if (isP2Cut)
                {
                    ep2Img.IsVisible = false;
                    ep2DirectedImg.IsVisible = false;
                }
                else
                {
                    ep2Img.IsVisible = true;
                    ep2Img.Source = GetEdgeBitmap(defaultPort2);
                    Canvas.SetLeft(ep2Img, cx2 - 80);
                    Canvas.SetTop(ep2Img, cy2 - 80);

                    if (IsConnectedTransition(unknown3))
                    {
                        ep2DirectedImg.IsVisible = true;
                        ep2DirectedImg.Source = GetRayBitmap(sock2);
                        Canvas.SetLeft(ep2DirectedImg, cx2 - 80);
                        Canvas.SetTop(ep2DirectedImg, cy2 - 80);
                    }
                    else
                    {
                        ep2DirectedImg.IsVisible = false;
                    }
                }

                // Stopper Caps (in their respective default endpoint boxes)
                bool p1Capped = !isP1Cut && ((u2_lo & 0xF0) == 0x70 || (unknown2 >= 0x0070 && unknown2 <= 0x0077));
                bool p2Capped = !isP2Cut && ((u3_lo & 0xF0) == 0x70 || (unknown3 >= 0x0070 && unknown3 <= 0x0077));

                if (p1Capped) AddCapAtPort(defaultPort1, tileSpan);
                if (p2Capped) AddCapAtPort(defaultPort2, tileSpan);
            }
            else if (typeId >= 4 && typeId < TrackPorts.Length)
            {
                // Curved and Y-shaped tracks: use full sprite
                trackCanvas.IsVisible = false;
                img.IsVisible = true;

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

                var (port1, port2, port3) = TrackPorts[typeId];

                // 1. Clipping for Cut Endpoints (0x0104)
                if (isP1Cut || isP2Cut)
                {
                    var group = new GeometryGroup();
                    group.Children.Add(new RectangleGeometry(new Rect(offset * 160, offset * 160, 160, 160)));

                    if (!isP1Cut)
                    {
                        (double cx1, double cy1) = GetPortCenter(port1, tileSpan);
                        group.Children.Add(new RectangleGeometry(new Rect((int)(cx1 / 160) * 160, (int)(cy1 / 160) * 160, 160, 160)));
                    }

                    if (!isP2Cut)
                    {
                        (double cx2, double cy2) = GetPortCenter(port2, tileSpan);
                        group.Children.Add(new RectangleGeometry(new Rect((int)(cx2 / 160) * 160, (int)(cy2 / 160) * 160, 160, 160)));
                    }

                    if (port3 != null)
                    {
                        (double cx3, double cy3) = GetPortCenter(port3.Value, tileSpan);
                        group.Children.Add(new RectangleGeometry(new Rect((int)(cx3 / 160) * 160, (int)(cy3 / 160) * 160, 160, 160)));
                    }

                    img.Clip = group;
                }
                else
                {
                    img.Clip = null;
                }

                if (port3 == null)
                {
                    bool p1Capped = !isP1Cut && ((u2_lo & 0xF0) == 0x70 || (unknown2 >= 0x0070 && unknown2 <= 0x0077));
                    bool p2Capped = !isP2Cut && ((u3_lo & 0xF0) == 0x70 || (unknown3 >= 0x0070 && unknown3 <= 0x0077));

                    if (p1Capped) AddCapAtPort(port1, tileSpan);
                    if (p2Capped) AddCapAtPort(port2, tileSpan);
                }
                else
                {
                    bool stemCapped = !isP1Cut && ((u2_lo & 0xF0) == 0x70 || (unknown2 >= 0x0070 && unknown2 <= 0x0077));
                    bool fork1Capped = typeId >= 12 ? (u2_hi & 0x10) == 0 : (u2_hi & 0x80) != 0;
                    bool fork2Capped = typeId >= 12 ? (u3_hi & 0x01) != 0 : (u3_lo & 0x40) == 0;

                    if (stemCapped) AddCapAtPort(port1, tileSpan);
                    if (fork1Capped) AddCapAtPort(port2, tileSpan);
                    if (fork2Capped) AddCapAtPort(port3.Value, tileSpan);
                }
            }
            else
            {
                img.Clip = null;
                trackCanvas.IsVisible = false;
                capCanvas.Children.Clear();
            }
        }

        public static readonly (TrackSocket port1, TrackSocket port2, TrackSocket? port3)[] TrackPorts = new[]
        {
            /* 0  horizontal        */ (TrackSocket.East, TrackSocket.West, (TrackSocket?)null),
            /* 1  vertical          */ (TrackSocket.North, TrackSocket.South, (TrackSocket?)null),
            /* 2  slope_up_right    */ (TrackSocket.SouthEast, TrackSocket.NorthWest, (TrackSocket?)null),
            /* 3  slope_down_right  */ (TrackSocket.NorthEast, TrackSocket.SouthWest, (TrackSocket?)null),
            /* 4  curve_upper_left  */ (TrackSocket.NorthWest, TrackSocket.SouthEast, (TrackSocket?)null),
            /* 5  curve_upper_right */ (TrackSocket.NorthWest, TrackSocket.SouthEast, (TrackSocket?)null),
            /* 6  curve_lower_right */ (TrackSocket.SouthWest, TrackSocket.NorthEast, (TrackSocket?)null),
            /* 7  curve_lower_left  */ (TrackSocket.SouthWest, TrackSocket.NorthEast, (TrackSocket?)null),
            /* 8  y_shape_up_left   */ (TrackSocket.West, TrackSocket.NorthEast, TrackSocket.SouthEast),
            /* 9  y_shape_up_right  */ (TrackSocket.East, TrackSocket.NorthWest, TrackSocket.SouthWest),
            /* 10 y_shape_down_left */ (TrackSocket.South, TrackSocket.NorthWest, TrackSocket.NorthEast),
            /* 11 y_shape_down_right*/ (TrackSocket.North, TrackSocket.SouthWest, TrackSocket.SouthEast),
            /* 12 y_shape_right_down*/ (TrackSocket.West, TrackSocket.NorthEast, TrackSocket.SouthEast),
            /* 13 y_shape_left_down */ (TrackSocket.East, TrackSocket.NorthWest, TrackSocket.SouthWest),
            /* 14 y_shape_right_up  */ (TrackSocket.South, TrackSocket.NorthWest, TrackSocket.NorthEast),
            /* 15 y_shape_left_up   */ (TrackSocket.North, TrackSocket.SouthWest, TrackSocket.SouthEast),
        };

        private static Bitmap? capBitmap;

        private void AddCapAtPort(TrackSocket port, int tileSpan)
        {
            (double cx, double cy) = GetPortCenter(port, tileSpan);
            Control cap = CreateStopperCap();
            Canvas.SetLeft(cap, cx - 80);
            Canvas.SetTop(cap, cy - 80);
            capCanvas.Children.Add(cap);
        }

        public static (double cx, double cy) GetPortCenter(TrackSocket socket, int tileSpan)
        {
            int tx = socket switch
            {
                TrackSocket.West or TrackSocket.NorthWest or TrackSocket.SouthWest => 0,
                TrackSocket.East or TrackSocket.SouthEast or TrackSocket.NorthEast => tileSpan - 1,
                _ => tileSpan / 2
            };

            int ty = socket switch
            {
                TrackSocket.North or TrackSocket.NorthWest or TrackSocket.NorthEast => 0,
                TrackSocket.South or TrackSocket.SouthEast or TrackSocket.SouthWest => tileSpan - 1,
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

        public static bool IsConnectedTransition(ushort unk)
        {
            if (unk == 0x0104) return false;
            byte lo = (byte)(unk & 0xFF);
            if ((lo & 0xF0) == 0x70 || (lo & 0xF0) == 0x80) return false;
            return (unk >= 0x0090 && unk <= 0x00A7) || unk == 0x0054 || unk == 0x0055 || unk == 0x0064 || unk == 0x0065 || (lo >= 0x90 && lo <= 0xA7);
        }

        public static TrackSocket ResolveEndpointSocket(ushort unk, TrackSocket defaultSocket)
        {
            return unk switch
            {
                0x0090 => TrackSocket.East,
                0x0091 => TrackSocket.North,
                0x0092 => TrackSocket.SouthEast,
                0x0093 => TrackSocket.NorthEast,

                0x0094 => TrackSocket.East,
                0x0095 => TrackSocket.East,
                0x0096 => TrackSocket.South,
                0x0097 => TrackSocket.North,

                0x0098 => TrackSocket.NorthEast,
                0x0099 => TrackSocket.SouthEast,
                0x009A => TrackSocket.North,
                0x009B => TrackSocket.North,
                0x009C => TrackSocket.East,
                0x009D => TrackSocket.East,
                0x009E => TrackSocket.East,
                0x009F => TrackSocket.NorthEast,
                0x00A0 => TrackSocket.South,
                0x00A1 => TrackSocket.South,
                0x00A2 => TrackSocket.NorthEast,
                0x00A3 => TrackSocket.SouthEast,
                0x00A4 => TrackSocket.NorthWest,
                0x00A5 => TrackSocket.NorthEast,
                0x00A6 => TrackSocket.SouthEast,
                0x00A7 => TrackSocket.West,

                0x0054 => TrackSocket.NorthEast,
                0x0055 => TrackSocket.SouthEast,
                0x0064 => TrackSocket.NorthWest,
                0x0065 => TrackSocket.NorthEast,

                _ => (unk & 0x0F) <= 7 && (((unk & 0xFF) & 0xF0) == 0x70 || ((unk & 0xFF) & 0xF0) == 0x80)
                    ? (TrackSocket)(unk & 0x0F)
                    : defaultSocket
            };
        }

        private static Bitmap? GetCenterBitmap(int typeId)
        {
            if (!centerBitmaps.TryGetValue(typeId, out var bmp))
            {
                bmp = AssetHelper.LoadBitmap($"Assets/sprites/parts/center_{typeId}.png");
                if (bmp != null) centerBitmaps[typeId] = bmp;
            }
            return bmp;
        }

        private static Bitmap? GetEdgeBitmap(TrackSocket socket)
        {
            if (!edgeBitmaps.TryGetValue(socket, out var bmp))
            {
                bmp = AssetHelper.LoadBitmap($"Assets/sprites/parts/edge_{(int)socket}.png");
                if (bmp != null) edgeBitmaps[socket] = bmp;
            }
            return bmp;
        }

        private static Bitmap? GetRayBitmap(TrackSocket direction)
        {
            // Maps outgoing ray direction D to the edge tile connecting center to border D
            TrackSocket raySocket = direction switch
            {
                TrackSocket.North => TrackSocket.South,         // edge_3
                TrackSocket.South => TrackSocket.North,         // edge_2
                TrackSocket.East  => TrackSocket.West,          // edge_0
                TrackSocket.West  => TrackSocket.East,          // edge_1
                TrackSocket.NorthWest => TrackSocket.SouthEast, // edge_5
                TrackSocket.SouthEast => TrackSocket.NorthWest, // edge_4
                TrackSocket.NorthEast => TrackSocket.SouthWest, // edge_6
                TrackSocket.SouthWest => TrackSocket.NorthEast, // edge_7
                _ => direction
            };
            return GetEdgeBitmap(raySocket);
        }
    }
}
