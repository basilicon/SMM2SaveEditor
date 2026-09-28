using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Kaitai;
using SMM2SaveEditor.Entities;

namespace SMM2SaveEditor.Utility
{
    public enum SlotHealthStatus
    {
        Empty,
        Healthy,
        HiddenInCoursebot,
        Corrupted
    }

    public enum DiagnosticSeverity
    {
        Pass,
        Info,
        Warning,
        Error
    }

    public class DiagnosticItem
    {
        public string Name { get; set; } = "";
        public DiagnosticSeverity Severity { get; set; } = DiagnosticSeverity.Pass;
        public string Message { get; set; } = "";
        public string Details { get; set; } = "";

        public string Icon => Severity switch
        {
            DiagnosticSeverity.Pass => "✔️",
            DiagnosticSeverity.Info => "ℹ️",
            DiagnosticSeverity.Warning => "⚠️",
            DiagnosticSeverity.Error => "❌",
            _ => "•"
        };
    }

    public class CourseHealthReport
    {
        public SlotHealthStatus Status { get; set; } = SlotHealthStatus.Empty;
        public bool IsOccupiedInSave { get; set; }
        public bool CourseFileExists { get; set; }
        public bool ThumbnailFileExists { get; set; }
        public bool CanUnhide { get; set; }
        public bool CanRepairThumbnail { get; set; }
        public bool CanRepairCourse { get; set; }
        public bool CanSanitizeFlags => CanRepairCourse;
        public int FlagViolationCount { get; set; }
        public string PrimaryReason { get; set; } = "";
        public List<DiagnosticItem> Checks { get; set; } = new();

        public void AddCheck(string name, DiagnosticSeverity severity, string message, string details = "")
        {
            Checks.Add(new DiagnosticItem
            {
                Name = name,
                Severity = severity,
                Message = message,
                Details = details
            });
        }
    }

    public static class CourseDiagnostics
    {
        public static CourseHealthReport DiagnoseSlot(string saveDir, int slotIndex, bool? isOccupiedInSave = null)
        {
            var report = new CourseHealthReport();
            string fileName = $"course_data_{slotIndex:D3}.bcd";
            string thumbFileName = $"course_thumb_{slotIndex:D3}.btl";

            string coursePath = Path.Combine(saveDir, fileName);
            string thumbPath = Path.Combine(saveDir, thumbFileName);

            report.CourseFileExists = File.Exists(coursePath);
            report.ThumbnailFileExists = File.Exists(thumbPath);
            report.IsOccupiedInSave = isOccupiedInSave ?? SaveDataCrypto.GetSlotStatus(saveDir, slotIndex);

            // 1. Check Coursebot slot registration in save.dat
            if (report.IsOccupiedInSave)
            {
                report.AddCheck("save.dat Registration", DiagnosticSeverity.Pass,
                    "Active (status = 1)",
                    "Slot is marked as occupied in save.dat. SMM2 Coursebot will attempt to display this slot.");
            }
            else
            {
                if (report.CourseFileExists)
                {
                    report.AddCheck("save.dat Registration", DiagnosticSeverity.Warning,
                        "Inactive (status = 0) - Course is HIDDEN in Coursebot",
                        "Coursebot explicitly checks offset 0xB920+(slot*8)+1 in save.dat. Because status is 0, the game skips this slot entirely and hides it from the Coursebot grid.");
                }
                else
                {
                    report.AddCheck("save.dat Registration", DiagnosticSeverity.Info,
                        "Available (status = 0)",
                        "Slot is marked empty in save.dat.");
                }
            }

            // Case A: No course file exists
            if (!report.CourseFileExists)
            {
                if (report.IsOccupiedInSave)
                {
                    report.Status = SlotHealthStatus.Corrupted;
                    report.PrimaryReason = "Ghost slot: Marked active in save.dat but course_data file is missing on disk.";
                    report.AddCheck("Course File", DiagnosticSeverity.Error, "Missing on disk",
                        $"{fileName} not found in save folder, but save.dat indicates an active course. In-game access may crash or reset.");
                }
                else
                {
                    report.Status = SlotHealthStatus.Empty;
                    report.PrimaryReason = "Empty Slot";
                }
                return report;
            }

            // Case B: Course file exists - inspect integrity
            byte[] rawBcd;
            try
            {
                rawBcd = File.ReadAllBytes(coursePath);
            }
            catch (Exception ex)
            {
                report.Status = SlotHealthStatus.Corrupted;
                report.PrimaryReason = $"File read error: {ex.Message}";
                report.AddCheck("Course File Read", DiagnosticSeverity.Error, "Failed to read file", ex.Message);
                return report;
            }

            // Check B.1: File Size
            if (rawBcd.Length != 0x5C000)
            {
                report.Status = SlotHealthStatus.Corrupted;
                report.PrimaryReason = $"Invalid file size: {rawBcd.Length} bytes (expected exactly 376,832 / 0x5C000).";
                report.AddCheck("Course File Size", DiagnosticSeverity.Error,
                    $"{rawBcd.Length} bytes (expected 376,832)",
                    "SMM2 course loader (0x00e77cc0) enforces file size <= 376,832 bytes.");
                return report;
            }
            report.AddCheck("Course File Size", DiagnosticSeverity.Pass, "376,832 bytes (0x5C000)", "Matches authentic SMM2 encrypted course container size.");

            // Check B.2: Crypto & AES-CMAC
            byte[] decrypted;
            try
            {
                decrypted = LevelCrypto.DecryptLevel(rawBcd);
                report.AddCheck("Course Outer AES-CMAC", DiagnosticSeverity.Pass, "Integrity Verified",
                    "Outer AES-CMAC signature matches decrypted payload using KeyTables.Course.");
                report.AddCheck("Course Header CRC32", DiagnosticSeverity.Pass, "Checksum Matches",
                    "Header CRC32 at offset 0x08 matches 376,768-byte payload checksum.");
            }
            catch (Exception ex)
            {
                report.Status = SlotHealthStatus.Corrupted;
                report.PrimaryReason = $"Crypto/Checksum failure: {ex.Message}";
                report.AddCheck("Course Integrity (CMAC/CRC)", DiagnosticSeverity.Error, ex.Message,
                    "SMM2 verifies outer AES-CMAC and header CRC32 on load. If these fail, the game treats the course as corrupted and purges it.");
                return report;
            }

            // Check B.3: File Magic ("SCDL" at offset 0x0C in unencrypted course header)
            if (rawBcd.Length >= 0x10)
            {
                string magic = System.Text.Encoding.ASCII.GetString(rawBcd, 0x0C, 4);
                if (magic != "SCDL")
                {
                    report.Status = SlotHealthStatus.Corrupted;
                    report.PrimaryReason = $"Invalid magic signature '{magic}' (expected 'SCDL').";
                    report.AddCheck("Course Magic Signature", DiagnosticSeverity.Error,
                        $"Found '{magic}' instead of 'SCDL'",
                        "SMM2 level headers must begin with 'SCDL' at offset 0x0C.");
                    return report;
                }
                report.AddCheck("Course Magic Signature", DiagnosticSeverity.Pass, "'SCDL' Valid", "Standard Super Mario Maker 2 level header signature.");
            }

            // Check B.4: Level Deserialization & Bounds
            try
            {
                Level lvl = new Level();
                lvl.LoadFromStream(new KaitaiStream(decrypted));

                string style = lvl.gameStyle.ToString().ToUpper();
                if (!Enum.IsDefined(typeof(GameStyle), lvl.gameStyle))
                {
                    report.Status = SlotHealthStatus.Corrupted;
                    report.PrimaryReason = $"Unknown GameStyle '{style}'.";
                    report.AddCheck("Game Style", DiagnosticSeverity.Error, $"Invalid style: {style}", "Supported styles are SMB1, SMB3, SMW, NSMBU/NSMBW, SM3DW.");
                    return report;
                }
                report.AddCheck("Game Style", DiagnosticSeverity.Pass, style, $"Recognized game theme: {style}");

                // Coordinate bounds
                if (lvl.startY > 27 || lvl.goalY > 27)
                {
                    report.Status = SlotHealthStatus.Corrupted;
                    report.PrimaryReason = $"Start/Goal position out of bounds (Start Y: {lvl.startY}, Goal Y: {lvl.goalY}).";
                    report.AddCheck("World Coordinates", DiagnosticSeverity.Error,
                        $"Start Y: {lvl.startY}, Goal Y: {lvl.goalY} (Max: 27)",
                        "Player start or goal position exceeds the vertical world limit.");
                    return report;
                }
                report.AddCheck("World Coordinates", DiagnosticSeverity.Pass,
                    $"Start Y: {lvl.startY}, Goal Y: {lvl.goalY}", "Within valid vertical world boundary (0-27).");

                // Entity limits
                int owObj = lvl.overworld?.objects?.Count ?? 0;
                int swObj = lvl.subworld?.objects?.Count ?? 0;
                int owGround = lvl.overworld?.ground?.Count ?? 0;
                int swGround = lvl.subworld?.ground?.Count ?? 0;
                int owTracks = lvl.overworld?.tracks?.Count ?? 0;
                int swTracks = lvl.subworld?.tracks?.Count ?? 0;

                bool entityLimitExceeded = owObj > 2600 || swObj > 2600 || owGround > 4000 || swGround > 4000 || owTracks > 1500 || swTracks > 1500;
                if (entityLimitExceeded)
                {
                    report.Status = SlotHealthStatus.Corrupted;
                    report.PrimaryReason = "Course exceeds SMM2 entity pool memory limits.";
                    report.AddCheck("Entity Limits", DiagnosticSeverity.Error,
                        $"OW: {owObj}/2600 obj, {owGround}/4000 ground | SW: {swObj}/2600 obj, {swGround}/4000 ground",
                        "Object count exceeds maximum engine buffer size.");
                    return report;
                }
                report.AddCheck("Entity Limits", DiagnosticSeverity.Pass,
                    $"OW: {owObj} obj, {owGround} ground | SW: {swObj} obj, {swGround} ground",
                    "All entity counts within SMM2 engine limits.");

                // Check Course Initialization / Management Flags
                bool isMgmtInitialized = (lvl.unknownManagementFlags & 1) != 0;
                if (!isMgmtInitialized)
                {
                    report.Status = SlotHealthStatus.Corrupted;
                    report.PrimaryReason = "Course container is uninitialized (missing management flag 0x1 at offset 0x18).";
                    report.CanRepairCourse = true;
                    report.AddCheck("Course Initialization Flag", DiagnosticSeverity.Error,
                        $"0x{lvl.unknownManagementFlags:X8} (Bit 0x1 missing)",
                        "Offset 0x18 management flags must have bit 0x1 set. When 0, SMM2 treats the course file as an incomplete/uninitialized container and deletes it on boot.");
                }
                else
                {
                    report.AddCheck("Course Initialization Flag", DiagnosticSeverity.Pass,
                        $"0x{lvl.unknownManagementFlags:X8} (Initialized)",
                        "Course management initialization flag bit 0x1 is active.");
                }

                // Check Course Title
                if (string.IsNullOrWhiteSpace(lvl.levelName))
                {
                    report.CanRepairCourse = true;
                    report.AddCheck("Course Title", DiagnosticSeverity.Warning,
                        "Blank / Missing Title",
                        "Level name is empty. SMM2 Coursebot displays empty text; repair will assign 'Untitled'.");
                }
                else
                {
                    report.AddCheck("Course Title", DiagnosticSeverity.Pass,
                        $"\"{lvl.levelName}\"",
                        "Valid course title.");
                }
            }
            catch (Exception ex)
            {
                report.Status = SlotHealthStatus.Corrupted;
                report.PrimaryReason = $"Parser error: {ex.Message}";
                report.AddCheck("Level Parser", DiagnosticSeverity.Error, "Parsing failed", ex.Message);
                return report;
            }

            // Check C: Thumbnail Verification
            bool hasValidThumbImage = false;
            if (report.ThumbnailFileExists)
            {
                try
                {
                    byte[] rawBtl = File.ReadAllBytes(thumbPath);
                    if (rawBtl.Length != ThumbnailCrypto.BtlFileSize)
                    {
                        report.AddCheck("Thumbnail File Size", DiagnosticSeverity.Warning,
                            $"{rawBtl.Length} bytes (expected {ThumbnailCrypto.BtlFileSize})",
                            "Invalid thumbnail file size. SMM2 may fail to load the thumbnail preview.");
                        report.CanRepairThumbnail = true;
                        report.CanRepairCourse = true;
                    }
                    else
                    {
                        // Check outer CMAC & Decrypt
                        byte[] decThumb = ThumbnailCrypto.DecryptThumbnail(rawBtl);
                        report.AddCheck("Thumbnail Outer CMAC", DiagnosticSeverity.Pass, "Integrity Verified",
                            "Outer AES-CMAC verified using KeyTables.Thumbnail.");

                        if (decThumb.Length >= 2 && decThumb[0] == 0xFF && decThumb[1] == 0xD8)
                        {
                            hasValidThumbImage = true;

                            // Inner HMAC verification
                            bool hmacOk = VerifyThumbnailInnerHmac(rawBtl);
                            if (hmacOk)
                            {
                                report.AddCheck("Thumbnail Inner HMAC-SHA256", DiagnosticSeverity.Pass, "Signature Valid",
                                    "Authentic Nintendo inner cryptographic HMAC-SHA256 signature verified.");
                            }
                            else
                            {
                                report.AddCheck("Thumbnail Inner HMAC-SHA256", DiagnosticSeverity.Error,
                                    "Signature MISMATCH (Tampered / Invalid)",
                                    "SMM2 checks this 32-byte HMAC-SHA256 digest at 0x1BFA0. If it fails, SMM2 detects thumbnail tampering and hides or resets the course in Coursebot!");
                                report.CanRepairThumbnail = true;
                                report.CanRepairCourse = true;
                            }

                            report.AddCheck("Thumbnail JPEG Format", DiagnosticSeverity.Pass, "Valid JFIF/JPEG",
                                $"Starts with standard SOI marker (0xFF 0xD8), payload size: {decThumb.Length} bytes.");
                        }
                        else
                        {
                            report.AddCheck("Thumbnail Container", DiagnosticSeverity.Warning, "Empty / No JPEG Image",
                                "Thumbnail file has no JPEG image data (0 bytes). SMM2 Coursebot requires a valid JPEG thumbnail to render the course.");
                            report.CanRepairThumbnail = true;
                            report.CanRepairCourse = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    report.AddCheck("Thumbnail Decryption", DiagnosticSeverity.Warning, "Decryption error", ex.Message);
                    report.CanRepairThumbnail = true;
                    report.CanRepairCourse = true;
                }
            }
            else
            {
                report.AddCheck("Thumbnail File", DiagnosticSeverity.Warning, "Missing",
                    $"{thumbFileName} does not exist in save directory. Coursebot displays a placeholder or uninitialized box.");
                report.CanRepairThumbnail = true;
                report.CanRepairCourse = true;
            }

            // If slot is occupied in save.dat but thumbnail has no valid JPEG, SMM2 deletes the course!
            if (report.IsOccupiedInSave && !hasValidThumbImage)
            {
                report.Status = SlotHealthStatus.Corrupted;
                if (string.IsNullOrEmpty(report.PrimaryReason))
                {
                    report.PrimaryReason = "Occupied course is missing a valid JPEG thumbnail (causes SMM2 Coursebot to report corruption and delete the course).";
                }
                report.CanRepairThumbnail = true;
                report.CanRepairCourse = true;
            }

            // Final Status Resolution
            if (!report.IsOccupiedInSave)
            {
                if (report.Status != SlotHealthStatus.Corrupted)
                {
                    report.Status = SlotHealthStatus.HiddenInCoursebot;
                    report.CanUnhide = true;
                    report.PrimaryReason = "Course data is valid, but hidden in Coursebot because save.dat marks this slot as inactive (status = 0).";
                }
            }
            else
            {
                if (report.Status != SlotHealthStatus.Corrupted)
                {
                    report.Status = SlotHealthStatus.Healthy;
                    report.PrimaryReason = "Course and slot registration are fully healthy.";
                }
            }

            return report;
        }

        private struct Random
        {
            public uint v0;
            public uint v1;
            public uint v2;
            public uint v3;

            public uint NextUInt(uint maxValue)
            {
                uint temp = v0;
                temp = (temp ^ (temp << 11)) & 0xFFFFFFFF;
                temp ^= temp >> 8;
                temp ^= v3;
                temp ^= v3 >> 19;
                v0 = v1;
                v1 = v2;
                v2 = v3;
                v3 = temp;

                return (uint)(((ulong)temp * (ulong)maxValue) >> 32);
            }
        }

        private static void CreateKey(ref Random random, uint[] table, int size, MemoryStream keyStream)
        {
            for (uint i = 0; i < size / 4; i++)
            {
                uint value = 0;
                for (int e = 0; e < 4; e++)
                {
                    uint index = random.NextUInt((uint)table.Length);
                    uint shift = random.NextUInt(4) * 8;
                    uint b = (table[(int)index] >> (int)shift) & 0xFF;
                    value = (value << 8) | b;
                }

                keyStream.Write(BitConverter.GetBytes(value), 0, sizeof(uint));
            }
        }

        private static bool VerifyThumbnailInnerHmac(byte[] rawBtl)
        {
            try
            {
                if (rawBtl.Length != ThumbnailCrypto.BtlFileSize) return false;

                // Decrypt full 114,640 body
                int end = ThumbnailCrypto.FooterOffset;
                Random r = new Random
                {
                    v0 = BitConverter.ToUInt32(rawBtl, end + 0x10),
                    v1 = BitConverter.ToUInt32(rawBtl, end + 0x14),
                    v2 = BitConverter.ToUInt32(rawBtl, end + 0x18),
                    v3 = BitConverter.ToUInt32(rawBtl, end + 0x1C),
                };

                MemoryStream aesKey = new MemoryStream();
                CreateKey(ref r, KeyTables.Thumbnail, 0x10, aesKey);

                using Aes aesBlock = Aes.Create();
                aesBlock.Key = aesKey.ToArray();
                aesBlock.Mode = CipherMode.CBC;
                aesBlock.Padding = PaddingMode.None;

                byte[] aesIv = rawBtl.Skip(end).Take(0x10).ToArray();
                using ICryptoTransform aesDecrypt = aesBlock.CreateDecryptor(aesBlock.Key, aesIv);

                byte[] body = new byte[ThumbnailCrypto.BodySize];
                aesDecrypt.TransformBlock(rawBtl, 0, ThumbnailCrypto.BodySize, body, 0);

                // Read inner seed at 0x1BFC0
                byte[] innerSeed = new byte[16];
                Buffer.BlockCopy(body, 0x1BFC0, innerSeed, 0, 16);

                Random innerR = new Random
                {
                    v0 = BitConverter.ToUInt32(innerSeed, 0),
                    v1 = BitConverter.ToUInt32(innerSeed, 4),
                    v2 = BitConverter.ToUInt32(innerSeed, 8),
                    v3 = BitConverter.ToUInt32(innerSeed, 12),
                };

                MemoryStream hmacKeyStream = new MemoryStream();
                CreateKey(ref innerR, KeyTables.Thumbnail, 0x10, hmacKeyStream);
                byte[] hmacKey = hmacKeyStream.ToArray();

                using var hmac = new HMACSHA256(hmacKey);
                byte[] computedDigest = hmac.ComputeHash(body, 0, 0x1BF9C);

                byte[] storedDigest = new byte[32];
                Buffer.BlockCopy(body, 0x1BFA0, storedDigest, 0, 32);

                return computedDigest.SequenceEqual(storedDigest);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// De-corrupts and repairs a course slot:
        /// 1. Ensures course initialization flag bit 0x1 is active at offset 0x18.
        /// 2. Sets level name to "Untitled" if empty or blank.
        /// 3. Ensures a valid JPEG thumbnail exists; generates a randomized noise placeholder with "No Thumbnail Found" if missing or empty.
        /// 4. Marks the slot as active/occupied in save.dat.
        /// </summary>
        public static bool RepairCourse(string saveDir, int slotIndex)
        {
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir)) return false;

            var targets = SaveManagerService.GetTargetDirectories(saveDir);
            string courseFile = $"course_data_{slotIndex:D3}.bcd";
            string thumbFile = $"course_thumb_{slotIndex:D3}.btl";

            bool repairedCourse = false;
            bool repairedThumb = false;

            // 1. Repair course data across all target directories
            foreach (var target in targets)
            {
                string bcdPath = Path.Combine(target, courseFile);
                if (File.Exists(bcdPath))
                {
                    try
                    {
                        byte[] rawBcd = File.ReadAllBytes(bcdPath);
                        byte[] dec = LevelCrypto.DecryptLevel(rawBcd);
                        Level lvl = new Level();
                        lvl.LoadFromStream(new KaitaiStream(dec));

                        bool modified = false;

                        // Ensure initialization flag bit 0x1 is set
                        if ((lvl.unknownManagementFlags & 1) == 0)
                        {
                            lvl.unknownManagementFlags |= 1;
                            modified = true;
                        }

                        // Ensure level name is not empty
                        if (string.IsNullOrWhiteSpace(lvl.levelName))
                        {
                            lvl.levelName = "Untitled";
                            modified = true;
                        }

                        if (modified)
                        {
                            byte[] reEncrypted = LevelCrypto.EncryptLevel(lvl.GetBytes());
                            File.WriteAllBytes(bcdPath, reEncrypted);
                            repairedCourse = true;
                        }
                        else
                        {
                            repairedCourse = true;
                        }
                    }
                    catch { }
                }
            }

            // 2. Ensure thumbnail is valid; generate random noise thumbnail if missing/empty/corrupted
            bool hasValidThumbnail = false;
            foreach (var target in targets)
            {
                string thumbPath = Path.Combine(target, thumbFile);
                if (File.Exists(thumbPath))
                {
                    try
                    {
                        byte[] rawBtl = File.ReadAllBytes(thumbPath);
                        if (rawBtl.Length == ThumbnailCrypto.BtlFileSize)
                        {
                            byte[] decThumb = ThumbnailCrypto.DecryptThumbnail(rawBtl);
                            if (decThumb.Length >= 2 && decThumb[0] == 0xFF && decThumb[1] == 0xD8)
                            {
                                hasValidThumbnail = true;
                                if (!VerifyThumbnailInnerHmac(rawBtl))
                                {
                                    byte[] fixedBtl = ThumbnailCrypto.EncryptThumbnail(decThumb);
                                    SaveManagerService.WriteThumbnail(saveDir, slotIndex, fixedBtl);
                                    repairedThumb = true;
                                }
                                break;
                            }
                        }
                    }
                    catch { }
                }
            }

            if (!hasValidThumbnail)
            {
                byte[] noiseBtl = ThumbnailCrypto.GeneratePlaceholderThumbnailBtl();
                SaveManagerService.WriteThumbnail(saveDir, slotIndex, noiseBtl);
                repairedThumb = true;
            }

            // 3. Ensure slot is marked active in save.dat
            SaveDataCrypto.SetSlotStatus(saveDir, slotIndex, occupied: true);

            return repairedCourse || repairedThumb;
        }

        /// <summary>
        /// Backwards-compatible alias for RepairCourse.
        /// </summary>
        public static bool SanitizeCourseFlags(string saveDir, int slotIndex) => RepairCourse(saveDir, slotIndex);

        /// <summary>
        /// Repairs an in-memory Level instance: ensures management flag bit 0x1 is set and assigns "Untitled" if name is blank.
        /// </summary>
        public static bool RepairLevel(Level lvl)
        {
            bool modified = false;
            if ((lvl.unknownManagementFlags & 1) == 0)
            {
                lvl.unknownManagementFlags |= 1;
                modified = true;
            }
            if (string.IsNullOrWhiteSpace(lvl.levelName))
            {
                lvl.levelName = "Untitled";
                modified = true;
            }
            return modified;
        }

        /// <summary>
        /// Backwards-compatible alias for RepairLevel.
        /// </summary>
        public static int SanitizeLevelFlags(Level lvl)
        {
            return RepairLevel(lvl) ? 1 : 0;
        }

        /// <summary>
        /// Runs engine integrity diagnostics on an in-memory Level instance, and optionally verifies its file on disk.
        /// </summary>
        public static CourseHealthReport DiagnoseLevel(Level lvl, string? filePath = null)
        {
            var report = new CourseHealthReport();

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                report.CourseFileExists = true;
                try
                {
                    byte[] rawBcd = File.ReadAllBytes(filePath);
                    if (rawBcd.Length != 0x5C000)
                    {
                        report.AddCheck("Course File Size", DiagnosticSeverity.Error,
                            $"{rawBcd.Length} bytes (expected 376,832)",
                            "SMM2 course loader enforces file size <= 376,832 bytes.");
                    }
                    else
                    {
                        report.AddCheck("Course File Size", DiagnosticSeverity.Pass, "376,832 bytes (0x5C000)", "Matches authentic SMM2 encrypted course container size.");
                    }

                    try
                    {
                        byte[] decrypted = LevelCrypto.DecryptLevel(rawBcd);
                        report.AddCheck("Course Outer AES-CMAC", DiagnosticSeverity.Pass, "Integrity Verified",
                            "Outer AES-CMAC signature matches decrypted payload using KeyTables.Course.");
                        report.AddCheck("Course Header CRC32", DiagnosticSeverity.Pass, "Checksum Matches",
                            "Header CRC32 at offset 0x08 matches payload checksum.");
                    }
                    catch (Exception ex)
                    {
                        report.AddCheck("Course Integrity (CMAC/CRC)", DiagnosticSeverity.Error, ex.Message,
                            "Failed outer AES-CMAC or header CRC32 check on disk.");
                    }

                    if (rawBcd.Length >= 0x10)
                    {
                        string magic = System.Text.Encoding.ASCII.GetString(rawBcd, 0x0C, 4);
                        if (magic != "SCDL")
                        {
                            report.AddCheck("Course Magic Signature", DiagnosticSeverity.Error,
                                $"Found '{magic}' instead of 'SCDL'",
                                "SMM2 level headers must begin with 'SCDL' at offset 0x0C.");
                        }
                        else
                        {
                            report.AddCheck("Course Magic Signature", DiagnosticSeverity.Pass, "'SCDL' Valid", "Standard Super Mario Maker 2 level header signature.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    report.AddCheck("Course File Read", DiagnosticSeverity.Error, "Read error", ex.Message);
                }
            }

            // In-Memory Level Validation
            string style = lvl.gameStyle.ToString().ToUpper();
            if (!Enum.IsDefined(typeof(GameStyle), lvl.gameStyle))
            {
                report.Status = SlotHealthStatus.Corrupted;
                report.PrimaryReason = $"Unknown GameStyle '{style}'.";
                report.AddCheck("Game Style", DiagnosticSeverity.Error, $"Invalid style: {style}", "Supported styles are SMB1, SMB3, SMW, NSMBU/NSMBW, SM3DW.");
            }
            else
            {
                report.AddCheck("Game Style", DiagnosticSeverity.Pass, style, $"Recognized game theme: {style}");
            }

            if (lvl.startY > 27 || lvl.goalY > 27)
            {
                report.Status = SlotHealthStatus.Corrupted;
                report.PrimaryReason = $"Start/Goal position out of bounds (Start Y: {lvl.startY}, Goal Y: {lvl.goalY}).";
                report.AddCheck("World Coordinates", DiagnosticSeverity.Error,
                    $"Start Y: {lvl.startY}, Goal Y: {lvl.goalY} (Max: 27)",
                    "Player start or goal position exceeds the vertical world limit.");
            }
            else
            {
                report.AddCheck("World Coordinates", DiagnosticSeverity.Pass,
                    $"Start Y: {lvl.startY}, Goal Y: {lvl.goalY}", "Within valid vertical world boundary (0-27).");
            }

            int owObj = lvl.overworld?.objects?.Count ?? 0;
            int swObj = lvl.subworld?.objects?.Count ?? 0;
            int owGround = lvl.overworld?.ground?.Count ?? 0;
            int swGround = lvl.subworld?.ground?.Count ?? 0;
            int owTracks = lvl.overworld?.tracks?.Count ?? 0;
            int swTracks = lvl.subworld?.tracks?.Count ?? 0;

            bool entityLimitExceeded = owObj > 2600 || swObj > 2600 || owGround > 4000 || swGround > 4000 || owTracks > 1500 || swTracks > 1500;
            if (entityLimitExceeded)
            {
                report.Status = SlotHealthStatus.Corrupted;
                report.PrimaryReason = "Course exceeds SMM2 entity pool memory limits.";
                report.AddCheck("Entity Limits", DiagnosticSeverity.Error,
                    $"OW: {owObj}/2600 obj, {owGround}/4000 ground | SW: {swObj}/2600 obj, {swGround}/4000 ground",
                    "Object count exceeds maximum engine buffer size.");
            }
            else
            {
                report.AddCheck("Entity Limits", DiagnosticSeverity.Pass,
                    $"OW: {owObj} obj, {owGround} ground | SW: {swObj} obj, {swGround} ground",
                    "All entity counts within SMM2 engine limits.");
            }

            // Check Course Initialization / Management Flags
            bool isMgmtInitialized = (lvl.unknownManagementFlags & 1) != 0;
            if (!isMgmtInitialized)
            {
                report.Status = SlotHealthStatus.Corrupted;
                report.PrimaryReason = "Course container is uninitialized (missing management flag 0x1 at offset 0x18).";
                report.CanRepairCourse = true;
                report.AddCheck("Course Initialization Flag", DiagnosticSeverity.Error,
                    $"0x{lvl.unknownManagementFlags:X8} (Bit 0x1 missing)",
                    "Offset 0x18 management flags must have bit 0x1 set. When 0, SMM2 treats the course file as an incomplete/uninitialized container and deletes it on boot.");
            }
            else
            {
                report.AddCheck("Course Initialization Flag", DiagnosticSeverity.Pass,
                    $"0x{lvl.unknownManagementFlags:X8} (Initialized)",
                    "Course management initialization flag bit 0x1 is active.");
            }

            // Check Course Title
            if (string.IsNullOrWhiteSpace(lvl.levelName))
            {
                report.CanRepairCourse = true;
                report.AddCheck("Course Title", DiagnosticSeverity.Warning,
                    "Blank / Missing Title",
                    "Level name is empty. SMM2 Coursebot displays empty text; repair will assign 'Untitled'.");
            }
            else
            {
                report.AddCheck("Course Title", DiagnosticSeverity.Pass,
                    $"\"{lvl.levelName}\"",
                    "Valid course title.");
            }

            if (report.Status != SlotHealthStatus.Corrupted)
            {
                report.Status = SlotHealthStatus.Healthy;
                report.PrimaryReason = "Course data is valid and within engine limits.";
            }

            return report;
        }
    }
}
