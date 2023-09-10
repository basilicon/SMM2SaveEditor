using Kaitai;
using SMM2SaveEditor.Utility;
using System.Text;
using System;
using Avalonia.Controls;
using System.Diagnostics;
using Avalonia.Markup.Xaml;

namespace SMM2SaveEditor
{
    public partial class Level : Entity
    {
        public byte startY;
        public byte goalY;
        public short goalX;
        public short timer;

        public short year;
        public sbyte month;
        public sbyte day;
        public sbyte hour;
        public sbyte minute;

        public AutoscrollSpeed autoscrollSpeed;
        public ClearConditionCategory clearConditionCategory;
        public ClearCondition clearCondition;
        public short clearConditionMagnitude;
        public GameVersion gameVersion;
        public GameStyle gameStyle;

        // level management 
        public string levelName = ""; // MAX 66 CHARS
        public string levelDescription = ""; // MAX 202 CHARS
        public int clearAttempts;
        public int clearTime;

        // subworlds
        Map overworld = new();
        Map subworld = new();

        // unknown garbage
        public int unknownGameVersion;
        public int unknownManagementFlags;
        public uint unknownCreationId;
        public long unknownUploadId;
        byte[] unknown1 = new byte[0];
        public byte unknown2;

        private Grid levelGrid;

        public Level()
        {
            InitializeComponent();
            levelGrid = this.Find<Grid>("LevelGrid");
            if (levelGrid == null) throw new MissingMemberException("No level grid!");

            levelGrid.PointerPressed += OnClick;
        }

        public override void LoadFromStream(KaitaiStream io)
        {
            levelGrid.Children.RemoveAll(levelGrid.Children);

            // might not be necessary
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            Debug.WriteLine("Loading level...");

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
            levelGrid.Children.Add(overworld);
            Grid.SetColumn(overworld, 1);
            Grid.SetRow(overworld, 1);
            overworld.ParentEntity = this;
            overworld.LoadFromStream(io);

            subworld = new Map();
            levelGrid.Children.Add(subworld);
            Grid.SetColumn(subworld, 1);
            Grid.SetRow(subworld, 3);
            subworld.ParentEntity = this;
            subworld.LoadFromStream(io);

            Debug.WriteLine("Finished opening level.");
        }

        public override byte[] GetBytes()
        {
            Debug.WriteLine("Compiling level data...");

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
            bb.Append((byte)autoscrollSpeed);
            bb.Append((byte)clearConditionCategory);
            bb.Append((uint)clearCondition);
            bb.Append(unknownGameVersion);
            bb.Append(unknownManagementFlags);
            bb.Append(clearAttempts);
            bb.Append(clearTime);
            bb.Append(unknownCreationId);
            bb.Append(unknownUploadId);
            bb.Append((uint)gameVersion);
            bb.Append(unknown1);
            bb.Append((ushort)gameStyle);
            bb.Append(unknown2);

            
            byte[] levelNameChars = new UnicodeEncoding().GetBytes(levelName);
            byte[] levelNameBuffer = new byte[66];
            for (int i = 0; i < levelNameChars.Length; i++) levelNameBuffer[i] = levelNameChars[i];
            bb.Append(levelNameBuffer);

            byte[] descriptionChars = new UnicodeEncoding().GetBytes(levelDescription);
            byte[] descriptionBuffer = new byte[202];
            for (int i = 0; i < descriptionChars.Length; i++) descriptionBuffer[i] = descriptionChars[i];
            bb.Append(descriptionBuffer);

            bb.Append(overworld.GetBytes());
            bb.Append(subworld.GetBytes());

            return bb.GetBytes();
        }

        public override void UpdateSprite()
        {
            Width = Math.Max(overworld.Width, subworld.Width) + 1000 * 2;
            Height = overworld.Height + subworld.Height + 600 * 3;

            levelGrid.RowDefinitions = new RowDefinitions("600," + overworld.Height.ToString() + ",600," + Math.Max((int)subworld.Height, 0).ToString() + ",600");
            levelGrid.ColumnDefinitions = new ColumnDefinitions("1000," + Math.Max((int)overworld.Width, (int)subworld.Width).ToString() + ",1000");

            base.UpdateSprite();
        }
    }
}
