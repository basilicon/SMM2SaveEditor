using SMM2Level.Utility;
using SMM2Level.Entities;
using Kaitai;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SMM2Level
{
    public partial class Map : Entity
    {
        Theme theme;
        AutoscrollType autoscrollType;
        BoundaryType boundaryType;
        Orientation orientation;
        byte liquidEndHeight;
        LiquidMode liquidMode;
        LiquidSpeed liquidSpeed;
        byte liquidStartHeight;

        int boundaryRight;
        int boundaryLeft;
        int boundaryTop;
        int boundaryBottom;

        int unknownFlag;
        int unknown1;

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

        public override void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
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


            FillLists(objects, objectCount, io, canvas);
            FillLists(sounds, soundCount, io, canvas);
            FillLists(snakes, snakeBlockCount, io, canvas);
            FillLists(clearPipes, clearPipeCount, io, canvas);
            FillLists(piranhaCreepers, piranhaCreeperCount, io, canvas);
            FillLists(exclamationBlocks, exclamationMarkBlockCount, io, canvas);
            FillLists(trackBlocks, trackBlockCount, io, canvas);
            FillLists(ground, groundCount, io, canvas);
            FillLists(tracks, trackCount, io, canvas);
            FillLists(icicles, icicleCount, io, canvas);

            unknown2 = io.ReadBytes(3516);
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(0xFFFF);

            bb.Append(theme);
            bb.Append(autoscrollType);
            bb.Append(boundaryType);
            bb.Append(orientation);
            bb.Append(liquidEndHeight);
            bb.Append(liquidMode);
            bb.Append(liquidSpeed);
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
            bb.Append(unknown1); // troll
            bb.Append(ground.Count);
            bb.Append(tracks.Count);
            bb.Append(icicles.Count);

            bb.Append(GetBytesFromList(objects));
            bb.Append(GetBytesFromList(sounds));
            bb.Append(GetBytesFromList(snakes));
            bb.Append(GetBytesFromList(clearPipes));
            bb.Append(GetBytesFromList(piranhaCreepers));
            bb.Append(GetBytesFromList(exclamationBlocks));
            bb.Append(GetBytesFromList(trackBlocks));
            bb.Append(GetBytesFromList(ground));
            bb.Append(GetBytesFromList(tracks));
            bb.Append(GetBytesFromList(icicles));

            bb.Append(unknown2);

            return bb.GetBytes();
        }
    }
}
