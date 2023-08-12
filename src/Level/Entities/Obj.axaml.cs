using Kaitai;
using SMM2Level.Utility;
using Avalonia.Controls;

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

        public enum ObjId : ushort
        {
            goomba = 0,
            koopa = 1,
            piranha_flower = 2,
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
            big_mushroom = 44,
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
            slight_slope = 87,
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

            Canvas.SetLeft(this, x);
            Canvas.SetBottom(this, y);

            //transform.position = new Vector2(x / 160f, y / 160f);
            //gameObject.name = id.ToString();
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
