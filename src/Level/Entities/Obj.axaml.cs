using Kaitai;
using SMM2SaveEditor.Utility;
using Avalonia.Controls;
using System.Diagnostics;
using Avalonia.Media.Imaging;

using Image = Avalonia.Controls.Image;
using System.Collections.Generic;
using Avalonia.Media;

namespace SMM2SaveEditor.Entities
{
    public partial class Obj : UserControl, IEntity
    {
        public int x = 0;
        public int y = 0;
        public short unknown1 = 0;
        public byte width = 0;
        public byte height = 0;
        public uint flag = 0;
        public uint cflag = 0;
        public uint ex = 0;
        public ObjId id = 0;
        public ObjId cid = 0;
        public short lid = 0;
        public short sid = 0;

        private Image img;
        private Grid grid;

        public enum ObjId : ushort
        {
            Goomba = 0,
            Koopa = 1,
            PiranhaPlant = 2,
            HammerBro = 3,
            Block = 4,
            QuestionBlock = 5,
            HardBlock = 6,
            Ground = 7,
            Coin = 8,
            Pipe = 9,
            Spring = 10,
            Lift = 11,
            Thwomp = 12,
            Blaster = 13,
            MushroomPlatform = 14,
            BobOmb = 15,
            SemisolidPlatform = 16,
            Bridge = 17,
            PSwitch = 18,
            Pow = 19,
            Mushroom = 20,
            DonutBlock = 21,
            Cloud = 22,
            NoteBlock = 23,
            FireBar = 24,
            Spiny = 25,
            GoalGround = 26,
            Goal = 27,
            BuzzyBeetle = 28,
            HiddenBlock = 29,
            Lakitu = 30,
            LakituCloud = 31,
            BanzaiBill = 32,
            OneUp = 33,
            FireFlower = 34,
            SuperStar = 35,
            LavaLift = 36,
            StartingBrick = 37,
            StartingArrow = 38,
            Magikoopa = 39,
            SpikeTop = 40,
            Boo = 41,
            ClownCar = 42,
            Spikes = 43,
            MegaMush = 44,
            ShoeGoomba = 45,
            DryBones = 46,
            Cannon = 47,
            Blooper = 48,
            CastleBridge = 49,
            HopChop = 50,
            Skipsqueak = 51,
            Wiggler = 52,
            ConveyorBelt = 53,
            Burner = 54,
            Door = 55,
            CheepCheep = 56,
            Muncher = 57,
            RockyWrench = 58,
            Track = 59,
            LavaBubble = 60,
            ChainChomp = 61,
            Bowser = 62,
            IceBlock = 63,
            Vine = 64,
            Stingby = 65,
            Arrow = 66,
            OneWay = 67,
            Saw = 68,
            Player = 69,
            BigCoin = 70,
            Semisolid3DW = 71,
            KoopaCar = 72,
            Toad = 73,
            SpikeBall = 74,
            Stone = 75,
            Twister = 76,
            BoomBoom = 77,
            Pokey = 78,
            PBlock = 79,
            SprintPlatform = 80,
            SMB2Mushroom = 81,
            Donut = 82,
            Skewer = 83,
            SnakeBlock = 84,
            TrackBlock = 85,
            Charvaargh = 86,
            ShallowSlope = 87,
            SteepSlope = 88,
            CustomAutoBird = 89,
            CheckpointFlag = 90,
            Seesaw = 91,
            RedCoin = 92,
            ClearPipe = 93,
            ConveyorBeltSloped = 94,
            Key = 95,
            AntTrooper = 96,
            WarpBox = 97,
            BowserJr = 98,
            OnOffBlock = 99,
            DottedLineBlock = 100,
            WaterMarker = 101,
            MontyMole = 102,
            FishBone = 103,
            AngrySun = 104,
            SwingingClaw = 105,
            Tree = 106,
            PiranhaCreeper = 107,
            BlinkingBlock = 108,
            SoundEffect = 109,
            SpikeBlock = 110,
            Mechakoopa = 111,
            Crate = 112,
            MushroomTrampoline = 113,
            Porkupuffer = 114,
            ToadetteCage = 115,
            SuperHammer = 116,
            Bully = 117,
            Icicle = 118,
            ExclamationBlock = 119,
            Lemmy = 120,
            Morton = 121,
            Larry = 122,
            Wendy = 123,
            Iggy = 124,
            Roy = 125,
            Ludwig = 126,
            CannonBox = 127,
            PropellerBox = 128,
            GoombaMask = 129,
            BulletBillMask = 130,
            RedPowBox = 131,
            OnOffTrampoline = 132,
            None = 0xFFFF
        }

        public enum ObjFlag : uint
        {
            InPipe = 0x1,
            Wings = 0x2,
            AltForm22 = 0x4,
            DirectionA1 = 0x8,
            DirectionA2 = 0x10,
            FaceLeft = 0x20 | 0x40,
            PipeLeft = 0x20,
            PipeUp = 0x40,
            Always = 0x40 | 0x6000000, // PERMANENT FLAG
            IsContainer = 0x80,
            HasContents = 0x80,
            Stretch = 0x100,
            InClowncar = 0x200,
            OnATrack = 0x400,
            InStack = 0x1000,
            Medium = 0x2000,
            Big = 0x4000,
            Is2x2 = 0x4000,
            Parachute = 0x8000,
            InCloud = 0x10000 | 0x20000,
            AltForm23 = 0x40000 | 0x80000,
            LeftTracks = 0x100000,
            BeginEntryIndex = 0x1000000,
            VertTracks = 0x200000,
            ContEntryIndex = 0x200000 | 0x400000 | 0x800000 | 0x10000000,
            AltForm1N = 0x400000,
            AltForm3N = 0x800000,
            AltForm5N = 0x1000000,
            LWallHang = 0x2000000,
            GroundHang = 0x6000000,
            InClaw = 0x8000000,
        }

        public enum CannonRotation : uint
        {
            deg135 = 0x0,
            deg180 = 0x400000,
            deg225 = 0x800000,
            deg270 = 0xC00000,
            deg90 = 0x1000000,
        }

        public enum ArrowRotation : uint
        {
            Right       = 0x0,
            Down        = 0x0800000,
            Left        = 0x1000000,
            Up          = 0x1800000,
            DownRight   = 0x0400000,
            DownLeft    = 0x0C00000,
            UpRight     = 0x1C00000,
            UpLeft      = 0x1400000
        }

        public enum PipeRotation : uint
        {
            Right = 0,
            Left = 0x20,
            Up = 0x40,
            Down = 0x60,
        }

        public enum Sizes : uint
        {
            Small = 0,
            Medium = 0x2000,
            Big = 0x4000
        }

        // TODO: Set up bitmap lookup table
        private static Dictionary<string, Bitmap> bitmaps = new Dictionary<string, Bitmap>();
        private static Dictionary<ushort, object>? spriteMappings = null;

        public Obj()
        {
            InitializeComponent();
            img = this.Find<Image>("Sprite");
            grid = this.Find<Grid>("LayoutGrid");
        }

        public void LoadFromStream(KaitaiStream io, Canvas? canvas = null) 
        { 
            x = io.ReadS4le();
            y = io.ReadS4le();
            unknown1 = io.ReadS2le();
            width = io.ReadU1();
            height = io.ReadU1();
            flag = io.ReadU4le();
            cflag = io.ReadU4le();
            ex = io.ReadU4le();
            id = (ObjId)io.ReadS2le();
            cid = (ObjId)io.ReadS2le();
            lid = io.ReadS2le();
            sid = io.ReadS2le();

            UpdateSprite();
        }

        public byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(32);

            bb.Append(x);
            bb.Append(y);
            bb.Append(unknown1);
            bb.Append(width);
            bb.Append(height);
            bb.Append(flag);
            bb.Append(cflag);
            bb.Append(ex);
            bb.Append((ushort)id);
            bb.Append((ushort)cid);
            bb.Append(lid);
            bb.Append(sid);

            return bb.GetBytes();
        }

        // TODO: SIMPLIFY IMPLEMENTATION FOR MODDERS
        // sorry modders LUL
        public void UpdateSprite()
        {
            Canvas.SetLeft(this, x - 80 * width);
            Canvas.SetBottom(this, y - 80);
            SetValue(WidthProperty, width * 160);
            SetValue(HeightProperty, height * 160);

            grid.Children.RemoveAll(grid.Children);
            grid.RowDefinitions = new RowDefinitions("*");
            grid.ColumnDefinitions = new ColumnDefinitions("*");

            // TODO: not this
            grid.Children.Add(img);

            string stid = ((int)id).ToString();

            switch (id)
            {
                // 1 alternate sprite
                case ObjId.Goomba:
                case ObjId.Koopa:
                case ObjId.Block:
                case ObjId.Coin:
                case ObjId.Spring:
                case ObjId.BobOmb:
                case ObjId.Pow:
                case ObjId.Mushroom:
                case ObjId.NoteBlock:
                case ObjId.Goal:
                case ObjId.OneUp:
                case ObjId.LavaLift:
                case ObjId.ClownCar:
                case ObjId.DryBones:
                case ObjId.Blooper:
                case ObjId.Skipsqueak:
                case ObjId.Wiggler:
                case ObjId.CheepCheep:
                case ObjId.ChainChomp:
                case ObjId.SpikeBall:
                case ObjId.BoomBoom:
                case ObjId.PBlock:
                case ObjId.TrackBlock:
                case ObjId.Key:
                case ObjId.WarpBox:
                case ObjId.DottedLineBlock:
                case ObjId.BlinkingBlock:
                case ObjId.SuperHammer:
                case ObjId.Icicle:
                    if ((flag & 0x4) != 0)
                        SetSprite(stid + "A");
                    else
                        SetSprite(stid);
                    break;

                // weird cases

                case ObjId.SpikeTop: HandleWallHang(stid + ((flag & 0x4) == 0 ? "A" : "B")); break;
                case ObjId.PiranhaPlant: // these are still broken and i dont know why
                    HandleWallHang(stid + ((flag & 0x4) == 0 ? "A" : "B"));
                    HandleTallTiledObjects();
                    break;

                case ObjId.Vine:
                    HandleVineSprite();
                    break;

                case ObjId.Pipe:
                    HandlePipeSprite();
                    break;

                case ObjId.MushroomPlatform:
                    HandleMushroomPlatformSprite();
                    break;

                case ObjId.SemisolidPlatform:
                    HandleSemisolidSprite();
                    break;

                case ObjId.Seesaw:
                    HandleSeesawSprite();
                    break;

                case ObjId.Bridge:
                    HandlePlatformSprite(stid);
                    break;

                case ObjId.Blaster:
                    HandleLauncherSprite();
                    break;

                case ObjId.Lift:
                case ObjId.OnOffTrampoline:
                case ObjId.MushroomTrampoline:
                case ObjId.ConveyorBelt:
                    if ((flag & 0x4) == 0) HandlePlatformSprite(stid);
                    else HandlePlatformSprite(stid, "D", "E", "C");
                    break;

                case ObjId.SnakeBlock: HandleSnake(); break;
                case ObjId.SteepSlope: HandleSteepSlope(stid, "7"); break;
                case ObjId.ConveyorBeltSloped:
                    if ((flag & 0x4) == 0)
                        HandleSteepSlope(stid, "53A", "53B");
                    else
                        HandleSteepSlope(stid + "C", "53CA", "53CB");
                    break;

                case ObjId.ShallowSlope: HandleShallowSlope(); break;

                case ObjId.Semisolid3DW:
                    HandleNineTileSprite("71", "71A", "71B", "71C", "71D", "71E", "71F", "71G", "71H");
                    break;
                // rotations
                case ObjId.CheckpointFlag:
                case ObjId.OneWay:
                case ObjId.Skewer:
                    HandleOrthogonalRotation(stid); break;

                case ObjId.Cannon: HandleCannonRotation(); break;
                case ObjId.Arrow: HandleArrowRotation(); break;

                case ObjId.SpikeBlock:
                    Canvas.SetLeft(this, x - 80);
                    SetSprite(stid);
                    break;
                // no alternate sprite
                default:
                    SetSprite(stid);
                    break;
            }
        }

        private void HandleNineTileSprite(
            string topLeft = "", string topCenter = "", string topRight = "", 
            string midLeft = "", string midCenter = "", string midRight = "",
            string botLeft = "", string botCenter = "", string botRight = "",
            int width = -1, int height = -1)
        {
            if (width == -1) width = this.width;
            if (height == -1) height = this.height;

            grid.RowDefinitions = new RowDefinitions(ObjectExtensions.EqualSpacingDefinition(height));
            grid.ColumnDefinitions = new ColumnDefinitions(ObjectExtensions.EqualSpacingDefinition(width));

            CreateImage(topLeft, 0, 0);
            CreateImage(topRight, 0, width - 1);
            CreateImage(botLeft, height - 1, 0);
            CreateImage(botRight, height - 1, width - 1);

            for (int i = 1; i < height - 1; i++)
            {
                CreateImage(topCenter, 0, i);
                CreateImage(botCenter, height - 1, i);
            }
            for (int i = 1; i < width - 1; i++)
            {
                CreateImage(midLeft, i, 0);
                CreateImage(midRight, i, width - 1);
            }
            for (int i = 1; i < width - 1; i++) for (int j = 1; j < height - 1; j++)
                {
                    CreateImage(midCenter, j, i);
                }
        }

        private void HandleTallTiledObjects(int count = 2, Image? image = null)
        {
            if (image == null) image = img;

            grid.RowDefinitions = new RowDefinitions(ObjectExtensions.EqualSpacingDefinition(count));
            grid.Height = count * 160;
            Grid.SetRowSpan(image, count);
            Canvas.SetBottom(this, y);
        }

        private void HandleWideTiledObjected(int count = 2, Image? image = null)
        {
            if (image == null) image = img;

            grid.ColumnDefinitions = new ColumnDefinitions(ObjectExtensions.EqualSpacingDefinition(count));
            grid.Width = count * 160;
            Grid.SetColumnSpan(image, count);
        }

        private void HandleSnake()
        {
            string name = (flag & 0x4) == 0 ? "84" : "84A";
            grid.ColumnDefinitions = new ColumnDefinitions(ObjectExtensions.EqualSpacingDefinition(width));

            for (int i = 0; i < width; i++)
            {
                Image piece = new Image();
                grid.Children.Add(piece);
                Grid.SetColumn(piece, i);
                SetSprite(name, piece);
            }
        }

        private void HandleLauncherSprite()
        {
            grid.RowDefinitions = new RowDefinitions(ObjectExtensions.EqualSpacingDefinition(height));

            Grid.SetRowSpan(img, 2);
            SetSprite("13" + ((flag & 0x4) != 0 ? "B" : ""));

            string trunkSprite = "13" + ((flag & 0x4) == 0 ? "A" : "C");
            for (int i = 2; i < height; i++)
            {
                Image trunk = new();
                grid.Children.Add(trunk);
                Grid.SetRow(trunk, i);
                SetSprite(trunkSprite, trunk);
            }
        }

        private void HandleVineSprite()
        {
            grid.RowDefinitions = new RowDefinitions(ObjectExtensions.EqualSpacingDefinition(height));

            SetSprite("64A");

            for (int i = 1; i < height - 1; i++)
            {
                Image stalk = new();
                grid.Children.Add(stalk);
                Grid.SetRow(stalk, i);
                SetSprite("64", stalk);
            }

            Image bottom = new();
            grid.Children.Add(bottom);
            Grid.SetRow(bottom, height - 1);
            SetSprite("64B", bottom);
        }

        private void HandlePipeSprite()
        {
            string color = (flag & 0xC0000) switch
            {
                0x40000 => "R",
                0x80000 => "U",
                0xC0000 => "P",
                _ => ""
            };

            string pipeCap = "9" + color + (flag & 0x60) switch
            {
                0x20 => "B",
                0x40 => "",
                0x60 => "A",
                _ => "C"
            };

            bool vert = (flag & 0x40) != 0;

            string pipeBase = "9" + color + (vert ? "D" : "E");

            SetSprite(pipeCap);

            string definitions = ObjectExtensions.EqualSpacingDefinition(height);
            if (vert) grid.RowDefinitions = new RowDefinitions(definitions);
            else
            {
                grid.ColumnDefinitions = new ColumnDefinitions(definitions);
                SetValue(WidthProperty, height * 160);
                SetValue(HeightProperty, width * 160);
            }

            switch (flag & 0x60)
            {
                case 0x20: // left
                    Grid.SetColumn(img, 0);
                    for (int i = 1; i < height; i++)
                    {
                        Image newImage = new();
                        grid.Children.Add(newImage);
                        Grid.SetColumn(newImage, i);
                        SetSprite(pipeBase, newImage);
                    }
                    Canvas.SetLeft(this, x - 80 - (height - 1) * 160);
                    break;
                case 0x40: // up
                    Grid.SetRow(img, 0);
                    for (int i = 1; i < height; i++)
                    {
                        Image newImage = new();
                        grid.Children.Add(newImage);
                        Grid.SetRow(newImage, i);
                        SetSprite(pipeBase, newImage);
                    }
                    Canvas.SetLeft(this, x - 80);
                    break;
                case 0x60: // down
                    Grid.SetRow(img, height - 1);
                    for (int i = 0; i < height - 1; i++)
                    {
                        Image newImage = new();
                        grid.Children.Add(newImage);
                        Grid.SetRow(newImage, i);
                        SetSprite(pipeBase, newImage);
                    }
                    Canvas.SetBottom(this, y - 80 - (height - 1) * 160);
                    Canvas.SetLeft(this, x - 80 - (width - 1) * 160);
                    break;
                default:   // right
                    Grid.SetColumn(img, height - 1);
                    for (int i = 0; i < height - 1; i++)
                    {
                        Image newImage = new();
                        grid.Children.Add(newImage);
                        Grid.SetColumn(newImage, i);
                        SetSprite(pipeBase, newImage);
                    }
                    Canvas.SetLeft(this, x - 80);
                    Canvas.SetBottom(this, y - 80 - (width - 1) * 160);
                    break;
            }
        }

        private void HandleWallHang(string name)
        {
            string loc = name + (flag & 0x6000000) switch
            {
                0x2000000 => "2",
                0x4000000 => "4",
                0x6000000 => "6",
                _ => "0"
            };

            SetSprite(loc);
        }

        private void HandleOrthogonalRotation(string name)
        {
            string loc = name + (flag & 0xC00000) switch
            {
                0x400000 => "A",
                0x800000 => "B",
                0xC00000 => "C",
                _ => ""
            };

            SetSprite(loc);
        }

        private void HandleSemisolidSprite()
        {
            HandlePlatformSprite("16");

            // TODO: Add semisolid background sprite
            // TODO: Add semisolid variations
        }

        private void HandleSeesawSprite()
        {
            HandlePlatformSprite("91");

            Image centerPiece = new();
            grid.Children.Add(centerPiece);
            Grid.SetColumn(centerPiece, (width - 1) / 2);
            if (width % 2 == 0) Grid.SetColumnSpan(centerPiece, 2);
            SetSprite("91C", centerPiece);
        }

        private void HandleMushroomPlatformSprite()
        {
            HandlePlatformSprite("14");

            // Stalks can be centered
            Grid stalk = new Grid();
            grid.Children.Add(stalk);
            Grid.SetRow(stalk, 1);
            Grid.SetColumn(stalk, (width - 1) / 2);
            string stalkRowDefs = "*";
            for (int i = 1; i < height - 1; i++) stalkRowDefs += ",*";
            stalk.RowDefinitions = new RowDefinitions(stalkRowDefs);

            if (width % 2 == 1) Grid.SetColumnSpan(stalk, 1);
            else Grid.SetColumnSpan(stalk, 2);

            for (int i = 0; i < height - 1; i++)
            {
                Image stalkCell = new Image();
                stalk.Children.Add(stalkCell);
                Grid.SetColumn(stalkCell, 0);
                Grid.SetRow(stalkCell, i);
                SetSprite("14C", stalkCell);
            }
        }

        private void HandlePlatformSprite(string name, string leftSuffix = "A", string rightSuffix = "B", string centerSuffix="")
        {
            grid.RowDefinitions = new RowDefinitions($"*,{height - 1}*");
            grid.ColumnDefinitions = new ColumnDefinitions(ObjectExtensions.EqualSpacingDefinition(width));

            SetSprite(name + leftSuffix);

            Image rightCorner = new Image();
            grid.Children.Add(rightCorner);
            Grid.SetColumn(rightCorner, width - 1);
            SetSprite(name + rightSuffix, rightCorner);

            for (int i = 1; i < width - 1; i++)
            {
                Image center = new Image();
                grid.Children.Add(center);
                Grid.SetColumn(center, i);
                SetSprite(name + centerSuffix, center);
            }
        }

        private void HandleSteepSlope(string name, string leftPiece, string? rightPiece = null)
        {
            Canvas.SetLeft(this, x - 80);

            grid.RowDefinitions = new RowDefinitions(ObjectExtensions.EqualSpacingDefinition(height));
            grid.ColumnDefinitions = new ColumnDefinitions(ObjectExtensions.EqualSpacingDefinition(width));

            if (rightPiece == null) rightPiece = leftPiece;

            SetSprite(leftPiece);
            Image finalPiece = new();
            grid.Children.Add(finalPiece);
            Grid.SetColumn(finalPiece, width - 1);
            SetSprite(rightPiece, finalPiece);

            if ((flag & 0x200000) != 0) // UP LEFT
            {
                Grid.SetRow(finalPiece, height - 1);

                for (int i = 1; i < height; i++)
                {
                    Image slopePiece = new();
                    grid.Children.Add(slopePiece);
                    Grid.SetColumn(slopePiece, i);
                    Grid.SetRow(slopePiece, i - 1);
                    Grid.SetRowSpan(slopePiece, 2);
                    SetSprite(name + "A", slopePiece);
                }
            }
            else // UP RIGHT
            {
                int maxHeight = height - 1;
                Grid.SetRow(img, maxHeight);

                for (int i = 1; i < height; i++)
                {
                    Image slopePiece = new();
                    grid.Children.Add(slopePiece);
                    Grid.SetColumn(slopePiece, i);
                    Grid.SetRow(slopePiece, maxHeight - i);
                    Grid.SetRowSpan(slopePiece, 2);
                    SetSprite(name, slopePiece);
                }
            }
        }

        private void HandleShallowSlope()
        {
            Canvas.SetLeft(this, x - 80);

            grid.RowDefinitions = new RowDefinitions(ObjectExtensions.EqualSpacingDefinition(height));
            grid.ColumnDefinitions = new ColumnDefinitions(ObjectExtensions.EqualSpacingDefinition(width));

            SetSprite("7");
            Image finalPiece = new();
            grid.Children.Add(finalPiece);
            Grid.SetColumn(finalPiece, width - 1);
            SetSprite("7", finalPiece);

            if ((flag & 0x200000) != 0) // UP LEFT
            {
                Grid.SetRow(finalPiece, height - 1);

                for (int i = 1; i < height; i++)
                {
                    Image slopePiece = new();
                    grid.Children.Add(slopePiece);
                    Grid.SetColumn(slopePiece, i * 2 - 1);
                    Grid.SetRow(slopePiece, i - 1);
                    Grid.SetColumnSpan(slopePiece, 2);
                    Grid.SetRowSpan(slopePiece, 2);
                    SetSprite("87A", slopePiece);
                }
            }
            else // UP RIGHT
            {
                Grid.SetRow(img, height - 1);

                for (int i = 1; i < height; i++)
                {
                    Image slopePiece = new();
                    grid.Children.Add(slopePiece);
                    Grid.SetColumn(slopePiece, i * 2 - 1);
                    Grid.SetRow(slopePiece, height - i - 1);
                    Grid.SetRowSpan(slopePiece, 2);
                    Grid.SetColumnSpan(slopePiece, 2);
                    SetSprite("87", slopePiece);
                }
            }
        }

        private void HandleCannonRotation()
        {
            string name = (flag & 0x4) == 0 ? "47" : "47E";
            SetSprite(name);

            double rotation = (CannonRotation)(flag & 0x1C00000) switch
            {
                CannonRotation.deg135 => 135,
                CannonRotation.deg225 => 225,
                CannonRotation.deg270 => 270,
                CannonRotation.deg90 => 90,
                CannonRotation.deg180 => 180,
                _ => 0
            };

            double baseRotation = (flag & 0x6000000) switch
            {
                0x2000000 => 90,
                0x4000000 => 0,
                0x6000000 => 180,
                _ => 270
            };

            img.SetValue(
                RenderTransformProperty, 
                new RotateTransform(rotation + baseRotation + (((baseRotation == 90) || (baseRotation == 270)) ? 180 : 0))
                );

            Image cannonBase = new Image();
            grid.Children.Add(cannonBase);
            SetSprite(name + (baseRotation) switch
            {
                90 => "B",
                0 => "C",
                180 => "A",
                _ => "D"
            }, cannonBase);
            cannonBase.ZIndex = 5;
        }

        private void HandleArrowRotation()
        {
            SetSprite("66" + (ArrowRotation)(flag & 0x1C00000) switch
            {
                ArrowRotation.DownRight => "A",
                ArrowRotation.Down => "B",
                ArrowRotation.DownLeft => "C",
                ArrowRotation.Left => "D",
                ArrowRotation.UpLeft => "E",
                ArrowRotation.Up => "F",
                ArrowRotation.UpRight => "G",
                _ => ""
            });

        }

        private void HandleSprite()
        {
            object spriteMapping = FindSpriteMapping();

            
        }

        private object FindSpriteMapping()
        {
            if (spriteMappings == null)
            {
                throw new System.Exception("No sprite mapping defined.");
            }

            object myMapping;
            if (!spriteMappings.TryGetValue((ushort)id, out myMapping))
            {
                throw new System.Exception($"No valid sprite mapping for {id}!");
            }
            return myMapping;
        }

        private Image? CreateImage(string name = "", int rowIndex = 0, int columnIndex = 0)
        {
            // grid cannot contain image
            if (rowIndex >= grid.RowDefinitions.Count || columnIndex >= grid.ColumnDefinitions.Count) return null;

            Image image = new();
            grid.Children.Add(image);
            if (name != "") SetSprite(name, image);
            Grid.SetRow(image, rowIndex);
            Grid.SetColumn(image, columnIndex);

            return image;
        }

        private void SetSprite(string name, Image? image = null)
        {
            if (bitmaps.ContainsKey(name))
            {
                SetSprite(bitmaps[name], image);
                return;
            }

            string loc =
#if RELEASE                
                "./Assets/sprites/" +
#else
                "../../../Assets/sprites/" +
#endif
                name + ".png";

            try
            {
                Bitmap bitmap = new Bitmap(loc);
                SetSprite(bitmap, image);
                bitmaps.Add(name, bitmap);
            }
            catch
            {
                Debug.WriteLine($"Could not find a sprite for {id} at {loc}");
            }
        }

        private void SetSprite(Bitmap bitmap, Image? image = null)
        {
            if (image == null) 
            {
                if (img == null) return;
                SetSprite(bitmap, img);
                return;
            }

            image.Source = bitmap;
            image.PointerPressed += (this as IEntity).OnClick;
        }
    }
}
