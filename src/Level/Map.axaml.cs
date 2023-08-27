using SMM2SaveEditor.Utility;
using SMM2SaveEditor.Entities;
using Kaitai;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Diagnostics;

namespace SMM2SaveEditor
{
    public partial class Map : UserControl, IEntity
    {
        public Theme theme;
        public AutoscrollType autoscrollType;
        public BoundaryType boundaryType;
        public Orientation orientation;
        public byte liquidEndHeight;
        public LiquidMode liquidMode;
        public LiquidSpeed liquidSpeed;
        public byte liquidStartHeight;

        public int boundaryRight;
        public int boundaryLeft;
        public int boundaryTop;
        public int boundaryBottom;

        public int unknownFlag;
        public int unknown1;

        List<Obj> objects = new(); // 2600
        List<SoundEffect> sounds = new(); // 300
        List<Snake> snakes = new(); // 5
        List<ClearPipe> clearPipes = new(); // 200
        List<PiranhaCreeper> piranhaCreepers = new(); // 10
        List<ExclamationBlock> exclamationBlocks = new(); // 10
        List<TrackBlock> trackBlocks = new(); // 10
        List<Ground> ground = new(); // 4000
        List<Track> tracks = new(); // 1500
        List<Icicle> icicles = new(); // 30

        byte[] unknown2 = new byte[3516];

        private Canvas? myCanvas;

        public Map()
        {
            InitializeComponent();
            myCanvas = this.Find<Canvas>("MapCanvas");
        }

        public void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
        {
            theme = (Theme)io.ReadU1();
            autoscrollType = (AutoscrollType)io.ReadU1();
            boundaryType = (BoundaryType)io.ReadU1();
            orientation = (Orientation)io.ReadU1();
            liquidEndHeight = io.ReadU1();
            liquidMode = (LiquidMode)io.ReadU1();
            liquidSpeed = (LiquidSpeed)io.ReadU1();
            liquidStartHeight = io.ReadU1();
            boundaryRight = io.ReadS4le();
            boundaryTop = io.ReadS4le();
            boundaryLeft = io.ReadS4le();
            boundaryBottom = io.ReadS4le();
            unknownFlag = io.ReadS4le();
            int objectCount = io.ReadS4le();
            int soundCount = io.ReadS4le();
            int snakeBlockCount = io.ReadS4le();
            int clearPipeCount = io.ReadS4le();
            int piranhaCreeperCount = io.ReadS4le();
            int exclamationMarkBlockCount = io.ReadS4le();
            int trackBlockCount = io.ReadS4le();
            unknown1 = io.ReadS4le();
            int groundCount = io.ReadS4le();
            int trackCount = io.ReadS4le();
            int icicleCount = io.ReadS4le();

            LevelUtility.FillLists(ref objects, objectCount, io, myCanvas);
            LevelUtility.FillLists(ref sounds, soundCount, io, myCanvas);
            LevelUtility.FillLists(ref snakes, snakeBlockCount, io, myCanvas);
            LevelUtility.FillLists(ref clearPipes, clearPipeCount, io, myCanvas);
            LevelUtility.FillLists(ref piranhaCreepers, piranhaCreeperCount, io, myCanvas);
            LevelUtility.FillLists(ref exclamationBlocks, exclamationMarkBlockCount, io, myCanvas);
            LevelUtility.FillLists(ref trackBlocks, trackBlockCount, io, myCanvas);
            LevelUtility.FillLists(ref ground, groundCount, io, myCanvas);
            LevelUtility.FillLists(ref tracks, trackCount, io, myCanvas);
            LevelUtility.FillLists(ref icicles, icicleCount, io, myCanvas);

            unknown2 = io.ReadBytes(0xDBC);
        }

        public byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(0xFFFF);

            bb.Append((byte)theme);
            bb.Append((byte)autoscrollType);
            bb.Append((byte)boundaryType);
            bb.Append((byte)orientation);
            bb.Append(liquidEndHeight);
            bb.Append((byte)liquidMode);
            bb.Append((byte)liquidSpeed);
            bb.Append(liquidStartHeight);
            bb.Append(boundaryRight);
            bb.Append(boundaryTop);
            bb.Append(boundaryLeft);
            bb.Append(boundaryBottom);
            bb.Append(unknownFlag);
            bb.Append(objects.Count);
            bb.Append(sounds.Count);
            bb.Append(snakes.Count);
            bb.Append(clearPipes.Count);
            bb.Append(piranhaCreepers.Count);
            bb.Append(exclamationBlocks.Count);
            bb.Append(trackBlocks.Count);
            bb.Append(unknown1); // bruh
            bb.Append(ground.Count);
            bb.Append(tracks.Count);
            bb.Append(icicles.Count);

            bb.Append(LevelUtility.GetBytesFromList(objects));
            bb.Append(LevelUtility.GetBytesFromList(sounds));
            bb.Append(LevelUtility.GetBytesFromList(snakes));
            bb.Append(LevelUtility.GetBytesFromList(clearPipes));
            bb.Append(LevelUtility.GetBytesFromList(piranhaCreepers));
            bb.Append(LevelUtility.GetBytesFromList(exclamationBlocks));
            bb.Append(LevelUtility.GetBytesFromList(trackBlocks));
            bb.Append(LevelUtility.GetBytesFromList(ground));
            bb.Append(LevelUtility.GetBytesFromList(tracks));
            bb.Append(LevelUtility.GetBytesFromList(icicles));

            bb.Append(unknown2);

            return bb.GetBytes();
        }

        public void UpdateSprite()
        {
            throw new System.NotImplementedException();
        }
    }
}
