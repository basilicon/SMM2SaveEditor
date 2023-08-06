using UnityEngine;
using System.IO;
using Kaitai;
using SMM2Level.Utility;
using System.Text;
using System;
using System.Linq;
using Unity.VisualScripting;
using System.Collections.Generic;

namespace SMM2Level
{
    public class Level : MonoBehaviour
    {
        byte startY;
        byte goalY;
        short goalX;
        short timer;

        short year;
        sbyte month;
        sbyte day;
        sbyte hour;
        sbyte minute;

        AutoscrollSpeed autoscrollSpeed;
        ClearConditionCategory clearConditionCategory;
        ClearCondition clearCondition;
        short clearConditionMagnitude;
        GameVersion gameVersion;
        GameStyle gameStyle;

        // level management 
        string levelName; // MAX 66 CHARS
        string levelDescription; // MAX 202 CHARS
        int clearAttempts;
        int clearTime;

        // subworlds
        Map overworld;
        Map subworld;

        // unknown garbage
        int unknownGameVersion;
        int unknownManagementFlags;
        uint unknownCreationId;
        long unknownUploadId;
        byte[] unknown1;
        byte unknown2;

        public void LoadFromStream(KaitaiStream io)
        {
            Debug.Log("Loading level...");

            startY = io.ReadU1();
            goalY = io.ReadU1();
            goalX = io.ReadS2le();
            timer = io.ReadS2le();
            clearConditionMagnitude = io.ReadS2le();
            year = io.ReadS2le();
            month = io.ReadS1();
            day = io.ReadS1();
            hour = io.ReadS1();
            minute = io.ReadS1();
            autoscrollSpeed = (AutoscrollSpeed) io.ReadU1();
            clearConditionCategory = (ClearConditionCategory) io.ReadU1();
            clearCondition = (ClearCondition) io.ReadS4le();
            unknownGameVersion = io.ReadS4le();
            unknownManagementFlags = io.ReadS4le();
            clearAttempts = io.ReadS4le();
            clearTime = io.ReadS4le();
            unknownCreationId = io.ReadU4le();
            unknownUploadId = io.ReadS8le();
            gameVersion = (GameVersion)io.ReadS4le();
            unknown1 = io.ReadBytes(189);
            gameStyle = (GameStyle)io.ReadS2le();
            unknown2 = io.ReadU1();
            levelName = Encoding.Unicode.GetString(io.ReadBytes(66));
            levelDescription = Encoding.Unicode.GetString(io.ReadBytes(202));

            GameObject mapPrefab = Resources.Load<GameObject>("Prefabs/Map");

            if (overworld != null) Destroy(overworld.gameObject);
            GameObject overGO = Instantiate(mapPrefab, transform);
            overworld = overGO.GetComponent<Map>();
            overworld.LoadFromStream(io);

            if (subworld != null) Destroy(subworld);
            GameObject subGO = Instantiate(mapPrefab, transform);
            subworld = subGO.GetComponent<Map>();
            subworld.LoadFromStream(io);
        } 

        public void LoadFromFile(string path)
        {
            Debug.Log("Decrypting bytes from file...");

            byte[] bytes = File.ReadAllBytes(path);

            bytes = LevelCrypto.DecryptLevel(bytes);
            LoadFromStream(new KaitaiStream(bytes));
        }

        private void Start()
        {
            LoadFromFile("D:/codeThings/Switch/MM2-Script-Data/MM2 Expanded Toolkit/Save File & Levels/course_data_001.bcd");
        }

        public static void FillLists<T>(List<T> entities, int numEntities, int maxEntities, GameObject prefab, KaitaiStream io, Transform t) where T : Entity, new()
        {
            entities = new List<T>(numEntities);

            for (int i = 0; i < numEntities; i++)
            {
                GameObject go = Instantiate(prefab, t);
                entities.Add(go.GetComponent<T>());
                entities[i].LoadFromStream(io);
            }

            // toss remaining data
            // TODO: REPLACE WITH ARITHMETIC
            // io.ReadBytes(sizeof(T) * (maxEntities - numEntities));
            for (int i = numEntities; i < maxEntities; i++)
            {
                GameObject go = Instantiate(prefab);
                go.GetComponent<T>().LoadFromStream(io);
                Destroy(go);
            }
        }

        public static void FillLists<T>(List<T> entities, int numEntities, int maxEntities, string path, KaitaiStream io, Transform t) where T : Entity, new()
        {
            GameObject prefab = Resources.Load<GameObject>(path);

            Level.FillLists(entities, numEntities, maxEntities, prefab, io, t);
        }
    }
}
