using SMM2Level.Utility;
using SMM2Level.Entities;
using Kaitai;
using UnityEngine;
using System.Collections.Generic;

namespace SMM2Level
{
    public class Map : MonoBehaviour
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
        List<Sound> sounds = new(); // 300
        List<Snake> snakes = new(); // 5
        List<ClearPipe> clearPipes = new(); // 200
        List<PiranhaCreeper> piranhaCreepers = new(); // 10
        List<ExclamationBlock> exclamationBlocks = new(); // 10
        List<TrackBlock> trackBlocks = new(); // 10
        List<Ground> ground = new(); // 4000
        List<Track> tracks = new(); // 1500
        List<Icicle> icicles = new(); // 30

        byte[] unknown2;

        public void LoadFromStream(KaitaiStream io)
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


            FillLists(objects, objectCount, (int)Maxes.Obj, "Obj", io);
            FillLists(sounds, soundCount, (int)Maxes.Sound, "Sound", io);
            FillLists(snakes, snakeBlockCount, (int)Maxes.Snake, "Snake", io);
            FillLists(clearPipes, clearPipeCount, (int)Maxes.ClearPipe, "ClearPipe", io);
            FillLists(piranhaCreepers, piranhaCreeperCount, (int)Maxes.PiranhaCreeper, "PiranhaCreeper", io);
            FillLists(exclamationBlocks, exclamationMarkBlockCount, (int)Maxes.ExclamationBlock, "ExclamationBlock", io);
            FillLists(trackBlocks, trackBlockCount, (int)Maxes.TrackBlock, "TrackBlock", io);
            FillLists(ground, groundCount, (int)Maxes.Ground, "Ground", io);
            FillLists(tracks, trackCount, (int)Maxes.Track, "Track", io);
            FillLists(icicles, icicleCount, (int)Maxes.Icicle, "Icicle", io);

            unknown2 = io.ReadBytes(3516);
        }

        private void FillLists<T>(List<T> entities, int numEntities, int maxEntities, string entityPath, KaitaiStream io) where T : Entity, new()
        {
            Level.FillLists<T>(entities, numEntities, maxEntities, "Prefabs/Entities/" + entityPath, io, transform);
        }
    }
}
