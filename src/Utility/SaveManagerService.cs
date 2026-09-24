using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Kaitai;
using SMM2SaveEditor.Entities;

namespace SMM2SaveEditor.Utility
{
    public class SlotInfo
    {
        public int SlotIndex { get; set; }
        public string SlotId => $"{SlotIndex:D3}";
        public string DisplayName => $"Slot {SlotIndex:D3}";
        public string Title { get; set; } = "<Empty>";
        public string GameStyle { get; set; } = "";
        public string GameVersion { get; set; } = "";
        public int OverworldObjects { get; set; }
        public int SubworldObjects { get; set; }
        public DateTime? LastModified { get; set; }
        public bool Exists { get; set; }
        public bool IsCorrupted { get; set; }
        public string StatusSummary { get; set; } = "Empty";
        public string FilePath { get; set; } = "";
    }

    public static class SaveManagerService
    {
        public static string DetectSaveDirectory()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                
                // 1. Ryujinx default save path
                string ryujinxSaveRoot = Path.Combine(appData, "Ryujinx", "bis", "user", "save");
                if (Directory.Exists(ryujinxSaveRoot))
                {
                    foreach (var titleDir in Directory.GetDirectories(ryujinxSaveRoot))
                    {
                        if (Directory.GetDirectories(titleDir).Any(d => Path.GetFileName(d) == "0" || Path.GetFileName(d) == "1"))
                        {
                            return titleDir;
                        }
                        if (File.Exists(Path.Combine(titleDir, "save.dat")) || Directory.GetFiles(titleDir, "course_data_*.bcd").Length > 0)
                        {
                            return titleDir;
                        }
                    }
                }

                // 2. Yuzu / Suyu save path
                string yuzuSaveRoot = Path.Combine(appData, "yuzu", "nand", "user", "save");
                if (Directory.Exists(yuzuSaveRoot))
                {
                    var bcdFiles = Directory.GetFiles(yuzuSaveRoot, "course_data_*.bcd", SearchOption.AllDirectories);
                    if (bcdFiles.Length > 0)
                    {
                        return Path.GetDirectoryName(bcdFiles[0])!;
                    }
                }
            }
            catch { }

            return "";
        }

        public static List<string> GetTargetDirectories(string baseDir)
        {
            var targets = new List<string>();
            string dir0 = Path.Combine(baseDir, "0");
            string dir1 = Path.Combine(baseDir, "1");

            if (Directory.Exists(dir0) && Directory.Exists(dir1))
            {
                targets.Add(dir0);
                targets.Add(dir1);
            }
            else
            {
                targets.Add(baseDir);
            }

            return targets;
        }

        public static List<SlotInfo> ScanSlots(string baseDir, int maxSlots = 60)
        {
            var slots = new List<SlotInfo>();
            if (string.IsNullOrWhiteSpace(baseDir) || !Directory.Exists(baseDir))
                return slots;

            // Pick active directory (either 1 or 0 whichever is newer, or baseDir)
            string scanDir = baseDir;
            string dir0 = Path.Combine(baseDir, "0");
            string dir1 = Path.Combine(baseDir, "1");

            if (Directory.Exists(dir0) && Directory.Exists(dir1))
            {
                DateTime t0 = Directory.GetLastWriteTime(dir0);
                DateTime t1 = Directory.GetLastWriteTime(dir1);
                scanDir = t1 >= t0 ? dir1 : dir0;
            }

            for (int i = 0; i < maxSlots; i++)
            {
                string fileName = $"course_data_{i:D3}.bcd";
                string fullPath = Path.Combine(scanDir, fileName);

                var info = new SlotInfo
                {
                    SlotIndex = i,
                    FilePath = fullPath
                };

                if (File.Exists(fullPath))
                {
                    info.Exists = true;
                    info.LastModified = File.GetLastWriteTime(fullPath);

                    try
                    {
                        byte[] raw = File.ReadAllBytes(fullPath);
                        byte[] dec = LevelCrypto.DecryptLevel(raw);

                        Level lvl = new Level();
                        lvl.LoadFromStream(new KaitaiStream(dec));

                        info.Title = string.IsNullOrWhiteSpace(lvl.levelName) ? "(Untitled)" : lvl.levelName;
                        info.GameStyle = lvl.gameStyle.ToString().ToUpper();
                        info.GameVersion = lvl.gameVersion.ToString();
                        info.OverworldObjects = lvl.overworld.objects.Count;
                        info.SubworldObjects = lvl.subworld.objects.Count;
                        info.StatusSummary = $"{info.GameStyle} | OW: {info.OverworldObjects} | SW: {info.SubworldObjects}";
                    }
                    catch (Exception ex)
                    {
                        info.IsCorrupted = true;
                        info.Title = "[Corrupted/Unreadable]";
                        info.StatusSummary = $"Decrypt failed: {ex.Message}";
                    }
                }
                else
                {
                    info.Exists = false;
                    info.Title = "(Empty Slot)";
                    info.StatusSummary = "Unused";
                }

                slots.Add(info);
            }

            return slots;
        }

        public static void WriteSlot(string baseDir, int slotIndex, byte[] encryptedBcd)
        {
            var targets = GetTargetDirectories(baseDir);
            string fileName = $"course_data_{slotIndex:D3}.bcd";

            foreach (var targetDir in targets)
            {
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                string destPath = Path.Combine(targetDir, fileName);

                // Create backup of existing file
                if (File.Exists(destPath))
                {
                    string backupPath = destPath + ".bak";
                    try { File.Copy(destPath, backupPath, true); } catch { }
                }

                File.WriteAllBytes(destPath, encryptedBcd);
            }
        }

        public static string BackupSaveDirectory(string baseDir)
        {
            string backupDir = Path.Combine(baseDir, "Backups");
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            string zipFileName = $"SaveBackup_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            string zipPath = Path.Combine(backupDir, zipFileName);

            // Zip the contents of baseDir excluding the Backups folder
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var targets = GetTargetDirectories(baseDir);
                foreach (var dir in targets)
                {
                    string dirName = Path.GetFileName(dir);
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        string entryName = Path.Combine(dirName, Path.GetFileName(file));
                        zip.CreateEntryFromFile(file, entryName);
                    }
                }
            }

            return zipPath;
        }
    }
}
