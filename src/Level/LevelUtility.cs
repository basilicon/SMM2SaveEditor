namespace SMM2Level.Utility
{
    public enum AutoscrollSpeed : byte
    {
        x1 = 0,
        x2 = 1,
        x3 = 2
    }

    public enum ClearCondition : uint
    {
        none = 0,
        reach_the_goal_without_landing_after_leaving_the_ground = 137525990,
        reach_the_goal_after_defeating_at_least_all_mechakoopa = 199585683,
        reach_the_goal_after_defeating_at_least_all_cheep_cheep = 272349836,
        reach_the_goal_without_taking_damage = 375673178,
        reach_the_goal_as_boomerang_mario = 426197923,
        reach_the_goal_while_wearing_a_shoe = 436833616,
        reach_the_goal_as_fire_mario = 713979835,
        reach_the_goal_as_frog_mario = 744927294,
        reach_the_goal_after_defeating_at_least_all_larry = 751004331,
        reach_the_goal_as_raccoon_mario = 900050759,
        reach_the_goal_after_defeating_at_least_all_blooper = 947659466,
        reach_the_goal_as_propeller_mario = 976173462,
        reach_the_goal_while_wearing_a_propeller_box = 994686866,
        reach_the_goal_after_defeating_at_least_all_spike = 998904081,
        reach_the_goal_after_defeating_at_least_all_boom_boom = 1008094897,
        reach_the_goal_while_holding_a_koopa_shell = 1051433633,
        reach_the_goal_after_defeating_at_least_all_porcupuffer = 1061233896,
        reach_the_goal_after_defeating_at_least_all_charvaargh = 1062253843,
        reach_the_goal_after_defeating_at_least_all_bullet_bill = 1079889509,
        reach_the_goal_after_defeating_at_least_all_bully_bullies = 1080535886,
        reach_the_goal_while_wearing_a_goomba_mask = 1151250770,
        reach_the_goal_after_defeating_at_least_all_hop_chops = 1182464856,
        reach_the_goal_while_holding_a_red_pow_block_or_reach_the_goal_after_activating_at_least_all_red_pow_block = 1219761531,
        reach_the_goal_after_defeating_at_least_all_bob_omb = 1221661152,
        reach_the_goal_after_defeating_at_least_all_spiny_spinies = 1259427138,
        reach_the_goal_after_defeating_at_least_all_bowser_meowser = 1268255615,
        reach_the_goal_after_defeating_at_least_all_ant_trooper = 1279580818,
        reach_the_goal_on_a_lakitus_cloud = 1283945123,
        reach_the_goal_after_defeating_at_least_all_boo = 1344044032,
        reach_the_goal_after_defeating_at_least_all_roy = 1425973877,
        reach_the_goal_while_holding_a_trampoline = 1429902736,
        reach_the_goal_after_defeating_at_least_all_morton = 1431944825,
        reach_the_goal_after_defeating_at_least_all_fish_bone = 1446467058,
        reach_the_goal_after_defeating_at_least_all_monty_mole = 1510495760,
        reach_the_goal_after_picking_up_at_least_all_1_up_mushroom = 1656179347,
        reach_the_goal_after_defeating_at_least_all_hammer_bro = 1665820273,
        reach_the_goal_after_hitting_at_least_all_p_switch_or_reach_the_goal_while_holding_a_p_switch = 1676924210,
        reach_the_goal_after_activating_at_least_all_pow_block_or_reach_the_goal_while_holding_a_pow_block = 1715960804,
        reach_the_goal_after_defeating_at_least_all_angry_sun = 1724036958,
        reach_the_goal_after_defeating_at_least_all_pokey = 1730095541,
        reach_the_goal_as_superball_mario = 1780278293,
        reach_the_goal_after_defeating_at_least_all_pom_pom = 1839897151,
        reach_the_goal_after_defeating_at_least_all_peepa = 1969299694,
        reach_the_goal_after_defeating_at_least_all_lakitu = 2035052211,
        reach_the_goal_after_defeating_at_least_all_lemmy = 2038503215,
        reach_the_goal_after_defeating_at_least_all_lava_bubble = 2048033177,
        reach_the_goal_while_wearing_a_bullet_bill_mask = 2076496776,
        reach_the_goal_as_big_mario = 2089161429,
        reach_the_goal_as_cat_mario = 2111528319,
        reach_the_goal_after_defeating_at_least_all_goomba_galoomba = 2131209407,
        reach_the_goal_after_defeating_at_least_all_thwomp = 2139645066,
        reach_the_goal_after_defeating_at_least_all_iggy = 2259346429,
        reach_the_goal_while_wearing_a_dry_bones_shell = 2549654281,
        reach_the_goal_after_defeating_at_least_all_sledge_bro = 2694559007,
        reach_the_goal_after_defeating_at_least_all_rocky_wrench = 2746139466,
        reach_the_goal_after_grabbing_at_least_all_50_coin = 2749601092,
        reach_the_goal_as_flying_squirrel_mario = 2855236681,
        reach_the_goal_as_buzzy_mario = 3036298571,
        reach_the_goal_as_builder_mario = 3074433106,
        reach_the_goal_as_cape_mario = 3146932243,
        reach_the_goal_after_defeating_at_least_all_wendy = 3174413484,
        reach_the_goal_while_wearing_a_cannon_box = 3206222275,
        reach_the_goal_as_link = 3314955857,
        reach_the_goal_while_you_have_super_star_invincibility = 3342591980,
        reach_the_goal_after_defeating_at_least_all_goombrat_goombud = 3346433512,
        reach_the_goal_after_grabbing_at_least_all_10_coin = 3348058176,
        reach_the_goal_after_defeating_at_least_all_buzzy_beetle = 3353006607,
        reach_the_goal_after_defeating_at_least_all_bowser_jr = 3392229961,
        reach_the_goal_after_defeating_at_least_all_koopa_troopa = 3437308486,
        reach_the_goal_after_defeating_at_least_all_chain_chomp = 3459144213,
        reach_the_goal_after_defeating_at_least_all_muncher = 3466227835,
        reach_the_goal_after_defeating_at_least_all_wiggler = 3481362698,
        reach_the_goal_as_smb2_mario = 3513732174,
        reach_the_goal_in_a_koopa_clown_car_junior_clown_car = 3649647177,
        reach_the_goal_as_spiny_mario = 3725246406,
        reach_the_goal_in_a_koopa_troopa_car = 3730243509,
        reach_the_goal_after_defeating_at_least_all_piranha_plant_jumping_piranha_plant = 3748075486,
        reach_the_goal_after_defeating_at_least_all_dry_bones = 3797704544,
        reach_the_goal_after_defeating_at_least_all_stingby_stingbies = 3824561269,
        reach_the_goal_after_defeating_at_least_all_piranha_creeper = 3833342952,
        reach_the_goal_after_defeating_at_least_all_fire_piranha_plant = 3842179831,
        reach_the_goal_after_breaking_at_least_all_crates = 3874680510,
        reach_the_goal_after_defeating_at_least_all_ludwig = 3974581191,
        reach_the_goal_as_super_mario = 3977257962,
        reach_the_goal_after_defeating_at_least_all_skipsqueak = 4042480826,
        reach_the_goal_after_grabbing_at_least_all_coin = 4116396131,
        reach_the_goal_after_defeating_at_least_all_magikoopa = 4117878280,
        reach_the_goal_after_grabbing_at_least_all_30_coin = 4122555074,
        reach_the_goal_as_balloon_mario = 4153835197,
        reach_the_goal_while_wearing_a_red_pow_box = 4172105156,
        reach_the_goal_while_riding_yoshi = 4209535561,
        reach_the_goal_after_defeating_at_least_all_spike_top = 4269094462,
        reach_the_goal_after_defeating_at_least_all_banzai_bill = 4293354249
    }

    public enum GameStyle : uint
    {
        smb1 = 12621,
        smb3 = 13133,
        nsmbw = 21847,
        sm3dw = 22323,
        smw = 22349
    }

    public enum GameVersion : uint
    {
        v1_0_0 = 0,
        v1_0_1 = 1,
        v1_1_0 = 2,
        v2_0_0 = 3,
        v3_0_0 = 4,
        v3_0_1 = 5,
        unk = 33 // ???
    }

    public enum ClearConditionCategory : byte
    {
        none = 0,
        parts = 1,
        status = 2,
        actions = 3
    }

    // Map utility
    public enum BoundaryType : byte
    {
        built_above_line = 0,
        built_below_line = 1
    }

    public enum AutoscrollType : byte
    {
        none = 0,
        slow = 1,
        normal = 2,
        fast = 3,
        custom = 4
    }

    public enum Orientation : byte
    {
        horizontal = 0,
        vertical = 1
    }

    public enum Theme : byte
    {
        overworld = 0,
        underground = 1,
        castle = 2,
        airship = 3,
        underwater = 4,
        ghost_house = 5,
        snow = 6,
        desert = 7,
        sky = 8,
        forest = 9
    }

    public enum LiquidMode : byte
    {
        static_liquid = 0,
        rising_or_falling = 1,
        rising_and_falling = 2
    }

    public enum LiquidSpeed : byte
    {
        none = 0,
        x1 = 1, 
        x2 = 2,
        x3 = 3
    }

    public enum Maxes
    {
        Obj = 2600,
        SoundEffect = 300,
        Snake = 5,
        ClearPipe = 200,
        PiranhaCreeper = 10,
        ExclamationBlock = 10,
        TrackBlock = 10,
        Ground = 4000,
        Track = 1500,
        Icicle = 30,
        SnakeNode = 120,
        ClearPipeNode = 36,
        PiranhaCreeperNode = 20,
        ExclamationBlockNode = 10,
        TrackBlockNode = 10,
    }

    public enum Sizes
    {
        Level = 512,
        Map = 32,
        ClearPipeNode = 8,
        ClearPipe = 4 + Maxes.ClearPipeNode * ClearPipeNode,
        ExclamationBlockNode = 4,
        ExclamationBlock = 4 + Maxes.ExclamationBlockNode * ExclamationBlockNode,
        Ground = 4,
        Icicle = 4,
        Obj = 32,
        PiranhaCreeperNode = 4,
        PiranhaCreeper = 4 + Maxes.PiranhaCreeperNode * PiranhaCreeperNode,
        SnakeNode = 8,
        Snake = 4 + Maxes.SnakeNode * SnakeNode,
        SoundEffect = 4,
        Track = 12,
        TrackBlockNode = 4,
        TrackBlock = 4 + Maxes.TrackBlockNode * TrackBlockNode,
    }
}
