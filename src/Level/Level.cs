using System.IO;
using Kaitai;
using SMM2Level.Utility;
using System.Text;
using System;

namespace SMM2Level
{
    public class Level
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
        string levelName = ""; // MAX 66 CHARS
        string levelDescription = ""; // MAX 202 CHARS
        int clearAttempts;
        int clearTime;

        // subworlds
        Map overworld = new();
        Map subworld = new();

        // unknown garbage
        int unknownGameVersion;
        int unknownManagementFlags;
        uint unknownCreationId;
        long unknownUploadId;
        byte[] unknown1 = new byte[0];
        byte unknown2;

        public void LoadFromStream(KaitaiStream io)
        {
            // Debug.Log("Loading level...");

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

            overworld = new Map();
            overworld.LoadFromStream(io);

            subworld = new Map();
            subworld.LoadFromStream(io);
        }

        public byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(0x5BFD0 - 0x10);

            bb.Append(startY);
            bb.Append(goalY);
            bb.Append(goalX);
            bb.Append(timer);
            bb.Append(clearConditionMagnitude);
            bb.Append(year);
            bb.Append(month);
            bb.Append(day);
            bb.Append(hour);
            bb.Append(minute);
            bb.Append(autoscrollSpeed);
            bb.Append(clearConditionCategory);
            bb.Append(clearCondition);
            bb.Append(unknownGameVersion);
            bb.Append(unknownManagementFlags);
            bb.Append(clearAttempts);
            bb.Append(clearTime);
            bb.Append(unknownCreationId);
            bb.Append(unknownUploadId);
            bb.Append(gameVersion);
            bb.Append(unknown1);
            bb.Append(gameStyle);
            bb.Append(unknown2);

            char[] levelNameChars = new char[66];
            for (int i = 0; i < levelName.Length; i++) levelNameChars[i] = levelName[i];
            bb.Append(levelNameChars);

            char[] descriptionChars = new char[202];
            for (int i = 0; i < levelDescription.Length; i++) descriptionChars[i] = levelDescription[i];
            bb.Append(levelNameChars);

            bb.Append(overworld.GetBytes());
            bb.Append(subworld.GetBytes());

            return bb.GetBytes();
        }

        public void LoadFromFile(string path)
        {
            // Debug.Log("Decrypting bytes from file...");

            byte[] bytes = File.ReadAllBytes(path);

            bytes = LevelCrypto.DecryptLevel(bytes);
            LoadFromStream(new KaitaiStream(bytes));
        }

        private bool ByteArrayToFile(string fileName, byte[] byteArray)
        {
            try
            {
                using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(byteArray, 0, byteArray.Length);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in process: {0}", ex);
                return false;
            }
        }
    }
}
