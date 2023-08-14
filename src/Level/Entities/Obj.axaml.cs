using Kaitai;
using SMM2Level.Utility;
using Avalonia.Controls;
using System.Diagnostics;
using Avalonia.Media.Imaging;

using Image = Avalonia.Controls.Image;

namespace SMM2Level.Entities
{
    public partial class Obj : UserControl, IEntity
    {
        int x;
        int y;
        short unknown1;
        byte width;
        byte height;
        uint flag;
        uint cflag;
        int ex;
        ObjId id;
        ObjId cid;
        short lid;
        short sid;

        private Image img;
        private Grid grid;

        public enum ObjId : ushort
        {
            goomba = 0,
            koopa = 1,
            piranha_plant = 2,
            hammer_bro = 3,
            block = 4,
            question_block = 5,
            hard_block = 6,
            ground = 7,
            coin = 8,
            pipe = 9,
            spring = 10,
            lift = 11,
            thwomp = 12,
            bullet_bill_blaster = 13,
            mushroom_platform = 14,
            bob_omb = 15,
            semisolid_platform = 16,
            bridge = 17,
            p_switch = 18,
            pow = 19,
            super_mushroom = 20,
            donut_block = 21,
            cloud = 22,
            note_block = 23,
            fire_bar = 24,
            spiny = 25,
            goal_ground = 26,
            goal = 27,
            buzzy_beetle = 28,
            hidden_block = 29,
            lakitu = 30,
            lakitu_cloud = 31,
            banzai_bill = 32,
            one_up = 33,
            fire_flower = 34,
            super_star = 35,
            lava_lift = 36,
            starting_brick = 37,
            starting_arrow = 38,
            magikoopa = 39,
            spike_top = 40,
            boo = 41,
            clown_car = 42,
            spikes = 43,
            mega_mush = 44,
            shoe_goomba = 45,
            dry_bones = 46,
            cannon = 47,
            blooper = 48,
            castle_bridge = 49,
            hop_chop = 50,
            skipsqueak = 51,
            wiggler = 52,
            fast_conveyor_belt = 53,
            burner = 54,
            door = 55,
            cheep_cheep = 56,
            muncher = 57,
            rocky_wrench = 58,
            track = 59,
            lava_bubble = 60,
            chain_chomp = 61,
            bowser = 62,
            ice_block = 63,
            vine = 64,
            stingby = 65,
            arrow = 66,
            one_way = 67,
            saw = 68,
            player = 69,
            big_coin = 70,
            semisolid_3dw = 71,
            koopa_car = 72,
            toad = 73,
            spike_ball = 74,
            stone = 75,
            twister = 76,
            boom_boom = 77,
            pokey = 78,
            p_block = 79,
            sprint_platform = 80,
            smb2_mushroom = 81,
            donut = 82,
            skewer = 83,
            snake_block = 84,
            track_block = 85,
            charvaargh = 86,
            shallow_slope = 87,
            steep_slope = 88,
            custom_auto_bird = 89,
            checkpoint_flag = 90,
            seesaw = 91,
            red_coin = 92,
            clear_pipe = 93,
            conveyor_belt = 94,
            key = 95,
            ant_trooper = 96,
            warp_box = 97,
            bowser_jr = 98,
            on_off_block = 99,
            dotted_line_block = 100,
            water_marker = 101,
            monty_mole = 102,
            fish_bone = 103,
            angry_sun = 104,
            swinging_claw = 105,
            tree = 106,
            piranha_creeper = 107,
            blinking_block = 108,
            sound_effect = 109,
            spike_block = 110,
            mechakoopa = 111,
            crate = 112,
            mushroom_trampoline = 113,
            porkupuffer = 114,
            toadette_cage = 115,
            super_hammer = 116,
            bully = 117,
            icicle = 118,
            exclamation_block = 119,
            lemmy = 120,
            morton = 121,
            larry = 122,
            wendy = 123,
            iggy = 124,
            roy = 125,
            ludwig = 126,
            cannon_box = 127,
            propeller_box = 128,
            goomba_mask = 129,
            bullet_bill_mask = 130,
            red_pow_box = 131,
            on_off_trampoline = 132
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
            ex = io.ReadS4le();
            id = (ObjId)io.ReadS2le();
            cid = (ObjId)io.ReadS2le();
            lid = io.ReadS2le();
            sid = io.ReadS2le();

            Canvas.SetLeft(this, x - 80);
            Canvas.SetBottom(this, y - 80);
            SetValue(WidthProperty, width * 160);
            SetValue(HeightProperty, height * 160);

            HandleSprite();

            //Debug.WriteLine($"{id} at {x},{y} : {width}, {height}");
        }

        private void HandleSprite()
        {
            string stid = ((int)id).ToString();

            switch (id)
            {
                // 1 alternate sprite
                case ObjId.goomba:
                case ObjId.koopa:
                case ObjId.block:
                case ObjId.coin:
                case ObjId.spring:
                case ObjId.bob_omb:
                case ObjId.pow:
                case ObjId.note_block:
                case ObjId.goal:
                case ObjId.one_up:
                case ObjId.lava_lift:
                case ObjId.clown_car:
                case ObjId.dry_bones:
                case ObjId.blooper:
                case ObjId.skipsqueak:
                case ObjId.wiggler:
                case ObjId.cheep_cheep:
                case ObjId.chain_chomp:
                case ObjId.spike_ball:
                case ObjId.boom_boom:
                case ObjId.p_block:
                case ObjId.snake_block:
                case ObjId.track_block:
                case ObjId.shallow_slope:
                case ObjId.steep_slope:
                case ObjId.key:
                case ObjId.warp_box:
                case ObjId.dotted_line_block:
                case ObjId.blinking_block:
                case ObjId.super_hammer:
                case ObjId.icicle:
                    if ((flag & 0x4) != 0)
                        SetSprite(stid + "A");
                    else
                        SetSprite(stid);
                    break;

                // weird cases

                case ObjId.vine:
                    HandleVineSprite();
                    break;

                case ObjId.pipe:
                    HandlePipeSprite();
                    break;

                case ObjId.mushroom_platform:
                    HandleMushroomPlatformSprite();
                    break;

                case ObjId.semisolid_platform:
                    HandleSemisolidSprite();
                    break;

                case ObjId.bridge:
                    HandlePlatformSprite("17");
                    break;

                case ObjId.bullet_bill_blaster:
                    HandleLauncherSprite();
                    break;

                // no alternate sprite
                default:
                    SetSprite(stid);
                    break;
            }
        }

        private void HandleLauncherSprite()
        {
            grid.RowDefinitions = new RowDefinitions(EqualSpacingDefinition(height));

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
            grid.RowDefinitions = new RowDefinitions(EqualSpacingDefinition(height));

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

            string definitions = EqualSpacingDefinition(height);
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
                    Canvas.SetBottom(this, y - 80 - (width - 1) * 160);
                    break;
            }
        }

        private void HandleSemisolidSprite()
        {
            HandlePlatformSprite("16");

            // TODO: Add semisolid background sprite
            // TODO: Add semisolid variations
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

        private void HandlePlatformSprite(string name)
        {
            grid.RowDefinitions = new RowDefinitions($"*,{height - 1}*");
            grid.ColumnDefinitions = new ColumnDefinitions(EqualSpacingDefinition(width));

            SetSprite(name + "A");

            Image rightCorner = new Image();
            grid.Children.Add(rightCorner);
            Grid.SetRow(rightCorner, 0);
            Grid.SetColumn(rightCorner, width - 1);
            SetSprite(name + "B", rightCorner);

            for (int i = 1; i < width - 1; i++)
            {
                Image center = new Image();
                grid.Children.Add(center);
                Grid.SetRow(center, 0);
                Grid.SetColumn(center, i);
                SetSprite(name, center);
            }
        }

        private string EqualSpacingDefinition(int count)
        {
            string defs = "*";
            for (int i = 1; i < count; i++) defs += ",*";
            return defs;
        }

        private void SetSprite(string name, Image? image = null)
        {
            string loc = "../../../img/sprites/" + name + ".png";

            try
            {
                SetSprite(new Bitmap(loc), image);
            }
            catch
            {
                Debug.WriteLine($"Could not find a sprite for {id} at {loc}");
            }
        }

        private void SetSprite(Bitmap bitmap, Image? image = null)
        {
            if (image == null) img.Source = bitmap;
            else image.Source = bitmap;
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
            bb.Append(id);
            bb.Append(cid);
            bb.Append(lid);
            bb.Append(sid);

            return bb.GetBytes();
        }
    }
}
