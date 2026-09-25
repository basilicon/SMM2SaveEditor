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
        public string InGameSlot => $"{(SlotIndex / 4) + 1}-{(SlotIndex % 4) + 1}";
        public string Title { get; set; } = "<Empty>";
        public string GameStyle { get; set; } = "";
        public string GameVersion { get; set; } = "";
        public int OverworldObjects { get; set; }
        public int SubworldObjects { get; set; }
        public DateTime? LastModified { get; set; }
        public bool Exists { get; set; }
        public bool IsCorrupted { get; set; }
        public bool IsHiddenInCoursebot { get; set; }
        public bool IsOccupiedInSave { get; set; }
        public SlotHealthStatus HealthStatus { get; set; } = SlotHealthStatus.Empty;

        public string HealthBadgeText => HealthStatus switch
        {
            SlotHealthStatus.Healthy => "🟢 Active",
            SlotHealthStatus.HiddenInCoursebot => "⚠️ Hidden in Coursebot",
            SlotHealthStatus.Corrupted => "❌ Corrupted",
            _ => "⚪ Empty"
        };

        public string HealthBadgeBackground => HealthStatus switch
        {
            SlotHealthStatus.Healthy => "#193B26",
            SlotHealthStatus.HiddenInCoursebot => "#4A3210",
            SlotHealthStatus.Corrupted => "#421818",
            _ => "#242735"
        };

        public string HealthBadgeForeground => HealthStatus switch
        {
            SlotHealthStatus.Healthy => "#2ECC71",
            SlotHealthStatus.HiddenInCoursebot => "#F39C12",
            SlotHealthStatus.Corrupted => "#E74C3C",
            _ => "#6C7284"
        };

        public string HealthBadgeBorder => HealthStatus switch
        {
            SlotHealthStatus.Healthy => "#27AE60",
            SlotHealthStatus.HiddenInCoursebot => "#E67E22",
            SlotHealthStatus.Corrupted => "#C0392B",
            _ => "#32364A"
        };

        public bool CanUnhide => HealthStatus == SlotHealthStatus.HiddenInCoursebot;
        public bool CanRepairThumbnail => HealthReport?.CanRepairThumbnail ?? false;
        public string PrimaryCorruptionReason { get; set; } = "";
        public CourseHealthReport? HealthReport { get; set; }

        public bool HasThumbnail { get; set; }
        public string ThumbnailPath { get; set; } = "";
        public Avalonia.Media.Imaging.Bitmap? ThumbnailBitmap { get; set; }
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
            string cleanDir = baseDir.TrimEnd('\\', '/');

            // If user selected /0 or /1 directly, resolve parent
            string dirName = Path.GetFileName(cleanDir);
            string parent = Path.GetDirectoryName(cleanDir) ?? cleanDir;

            if ((dirName == "0" || dirName == "1") && Directory.Exists(Path.Combine(parent, "0")) && Directory.Exists(Path.Combine(parent, "1")))
            {
                targets.Add(Path.Combine(parent, "0"));
                targets.Add(Path.Combine(parent, "1"));
                return targets;
            }

            string dir0 = Path.Combine(cleanDir, "0");
            string dir1 = Path.Combine(cleanDir, "1");

            if (Directory.Exists(dir0) && Directory.Exists(dir1))
            {
                targets.Add(dir0);
                targets.Add(dir1);
            }
            else
            {
                targets.Add(cleanDir);
            }

            return targets;
        }

        public static List<SlotInfo> ScanSlots(string baseDir, int maxSlots = 60)
        {
            var slots = new List<SlotInfo>();
            if (string.IsNullOrWhiteSpace(baseDir) || !Directory.Exists(baseDir))
                return slots;

            string cleanDir = baseDir.TrimEnd('\\', '/');
            string dirName = Path.GetFileName(cleanDir);
            string parent = Path.GetDirectoryName(cleanDir) ?? cleanDir;

            // Pick active directory (either 1 or 0 whichever is newer, or cleanDir)
            string scanDir = cleanDir;
            string dir0 = Path.Combine(cleanDir, "0");
            string dir1 = Path.Combine(cleanDir, "1");

            if ((dirName == "0" || dirName == "1") && Directory.Exists(Path.Combine(parent, "0")) && Directory.Exists(Path.Combine(parent, "1")))
            {
                dir0 = Path.Combine(parent, "0");
                dir1 = Path.Combine(parent, "1");
            }

            if (Directory.Exists(dir0) && Directory.Exists(dir1))
            {
                DateTime t0 = Directory.GetLastWriteTime(dir0);
                DateTime t1 = Directory.GetLastWriteTime(dir1);
                scanDir = t1 >= t0 ? dir1 : dir0;
            }

            bool[] allStatuses = SaveDataCrypto.GetAllSlotStatuses(scanDir);

            for (int i = 0; i < maxSlots; i++)
            {
                string fileName = $"course_data_{i:D3}.bcd";
                string fullPath = Path.Combine(scanDir, fileName);

                var info = new SlotInfo
                {
                    SlotIndex = i,
                    FilePath = fullPath
                };

                // Run reverse-engineered engine integrity diagnostics
                bool slotOccupied = allStatuses != null && i < allStatuses.Length ? allStatuses[i] : false;
                var report = CourseDiagnostics.DiagnoseSlot(scanDir, i, slotOccupied);
                info.HealthReport = report;
                info.HealthStatus = report.Status;
                info.IsOccupiedInSave = report.IsOccupiedInSave;
                info.PrimaryCorruptionReason = report.PrimaryReason;
                info.IsHiddenInCoursebot = report.Status == SlotHealthStatus.HiddenInCoursebot;

                string thumbFileName = $"course_thumb_{i:D3}.btl";
                string thumbFullPath = Path.Combine(scanDir, thumbFileName);
                if (File.Exists(thumbFullPath))
                {
                    info.HasThumbnail = true;
                    info.ThumbnailPath = thumbFullPath;
                    try
                    {
                        byte[] btlBytes = File.ReadAllBytes(thumbFullPath);
                        byte[] imgBytes = ThumbnailCrypto.DecryptThumbnail(btlBytes);
                        if (imgBytes != null && imgBytes.Length > 0)
                        {
                            using var ms = new MemoryStream(imgBytes);
                            info.ThumbnailBitmap = new Avalonia.Media.Imaging.Bitmap(ms);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Thumbnail load failed for {thumbFileName}: {ex.Message}");
                    }
                }

                if (report.Status == SlotHealthStatus.Healthy || report.Status == SlotHealthStatus.HiddenInCoursebot)
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

                        string prefix = info.IsHiddenInCoursebot ? "[HIDDEN] " : "";
                        info.StatusSummary = $"{prefix}{info.GameStyle} | OW: {info.OverworldObjects} | SW: {info.SubworldObjects}";
                    }
                    catch (Exception ex)
                    {
                        info.IsCorrupted = true;
                        info.HealthStatus = SlotHealthStatus.Corrupted;
                        info.Title = "[Corrupted/Unreadable]";
                        info.StatusSummary = $"Decrypt failed: {ex.Message}";
                    }
                }
                else if (report.Status == SlotHealthStatus.Corrupted)
                {
                    info.Exists = true;
                    info.IsCorrupted = true;
                    if (File.Exists(fullPath))
                    {
                        info.LastModified = File.GetLastWriteTime(fullPath);
                    }
                    info.Title = "[Corrupted Slot]";
                    info.StatusSummary = report.PrimaryReason;
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

        public static void WriteSlot(string baseDir, int slotIndex, byte[] encryptedBcd, byte[]? optionalBtl = null)
        {
            var targets = GetTargetDirectories(baseDir);
            string fileName = $"course_data_{slotIndex:D3}.bcd";

            foreach (var targetDir in targets)
            {
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                string destPath = Path.Combine(targetDir, fileName);
                File.WriteAllBytes(destPath, encryptedBcd);
            }

            if (optionalBtl != null && optionalBtl.Length > 0)
            {
                WriteThumbnail(baseDir, slotIndex, optionalBtl);
            }
            else
            {
                // Ensure a thumbnail exists for this slot. If not, generate a valid blank container
                bool hasExistingThumb = false;
                string thumbName = $"course_thumb_{slotIndex:D3}.btl";
                foreach (var targetDir in targets)
                {
                    if (File.Exists(Path.Combine(targetDir, thumbName)))
                    {
                        hasExistingThumb = true;
                        break;
                    }
                }

                if (!hasExistingThumb)
                {
                    byte[] blankContainer = ThumbnailCrypto.EncryptThumbnail(Array.Empty<byte>());
                    WriteThumbnail(baseDir, slotIndex, blankContainer);
                }
            }

            // Automatically mark slot as OCCUPIED in save.dat
            SaveDataCrypto.SetSlotStatus(baseDir, slotIndex, occupied: true);
        }

        public static bool WriteThumbnail(string baseDir, int slotIndex, byte[] btlBytes)
        {
            var targets = GetTargetDirectories(baseDir);
            string fileName = $"course_thumb_{slotIndex:D3}.btl";

            foreach (var targetDir in targets)
            {
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                string destPath = Path.Combine(targetDir, fileName);
                File.WriteAllBytes(destPath, btlBytes);
            }

            return true;
        }

        public static bool CopyThumbnail(string baseDir, int srcSlotIndex, int dstSlotIndex)
        {
            var targets = GetTargetDirectories(baseDir);
            string srcFileName = $"course_thumb_{srcSlotIndex:D3}.btl";

            byte[]? btlBytes = null;
            foreach (var targetDir in targets)
            {
                string srcPath = Path.Combine(targetDir, srcFileName);
                if (File.Exists(srcPath))
                {
                    btlBytes = File.ReadAllBytes(srcPath);
                    break;
                }
            }

            if (btlBytes == null || btlBytes.Length == 0) return false;

            return WriteThumbnail(baseDir, dstSlotIndex, btlBytes);
        }

        public static bool DeleteThumbnail(string baseDir, int slotIndex)
        {
            // In SMM2, empty slots have a 114,688-byte encrypted container filled with zeros.
            // Completely removing the file causes the game to report a missing file / corruption!
            byte[] blankContainer = ThumbnailCrypto.EncryptThumbnail(Array.Empty<byte>());
            return WriteThumbnail(baseDir, slotIndex, blankContainer);
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
