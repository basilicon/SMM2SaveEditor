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
        public bool CanSanitizeFlags { get; set; }
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

                // Check B.5: Per-Actor Capability Masks & Object Flag Validation
                var flagErrors = new List<string>();
                if (lvl.overworld?.objects != null)
                {
                    for (int i = 0; i < lvl.overworld.objects.Count; i++)
                    {
                        var obj = lvl.overworld.objects[i];
                        var errs = ActorCapabilities.ValidateObjectFlags(obj.id, obj.flag, obj.cflag, i, "Overworld");
                        flagErrors.AddRange(errs);
                    }
                }
                if (lvl.subworld?.objects != null)
                {
                    for (int i = 0; i < lvl.subworld.objects.Count; i++)
                    {
                        var obj = lvl.subworld.objects[i];
                        var errs = ActorCapabilities.ValidateObjectFlags(obj.id, obj.flag, obj.cflag, i, "Subworld");
                        flagErrors.AddRange(errs);
                    }
                }

                report.FlagViolationCount = flagErrors.Count;
                if (flagErrors.Count > 0)
                {
                    report.Status = SlotHealthStatus.Corrupted;
                    report.CanSanitizeFlags = true;
                    if (string.IsNullOrEmpty(report.PrimaryReason))
                    {
                        report.PrimaryReason = $"{flagErrors.Count} object(s) have invalid flags violating SMM2 actor capabilities.";
                    }
                    report.AddCheck("Actor Capabilities & Flags", DiagnosticSeverity.Error,
                        $"{flagErrors.Count} illegal flag violation(s)",
                        string.Join("\n", flagErrors.Take(10)) + (flagErrors.Count > 10 ? $"\n...and {flagErrors.Count - 10} more" : ""));
                }
                else
                {
                    report.AddCheck("Actor Capabilities & Flags", DiagnosticSeverity.Pass,
                        "All object flags verified",
                        "All entities conform to official SMM2 actor capability masks and mutual exclusion rules.");
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
                    }
                    else
                    {
                        // Check outer CMAC & Decrypt
                        byte[] decThumb = ThumbnailCrypto.DecryptThumbnail(rawBtl);
                        report.AddCheck("Thumbnail Outer CMAC", DiagnosticSeverity.Pass, "Integrity Verified",
                            "Outer AES-CMAC verified using KeyTables.Thumbnail.");

                        // If not a blank container, check inner HMAC-SHA256 signature
                        if (decThumb.Length > 0 && !(rawBtl[0] == 0 && rawBtl[1] == 0 && rawBtl[2] == 0))
                        {
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
                            }

                            // Check JPEG header
                            if (decThumb.Length >= 2 && decThumb[0] == 0xFF && decThumb[1] == 0xD8)
                            {
                                report.AddCheck("Thumbnail JPEG Format", DiagnosticSeverity.Pass, "Valid JFIF/JPEG",
                                    "Starts with standard SOI marker (0xFF 0xD8).");
                            }
                            else
                            {
                                report.AddCheck("Thumbnail JPEG Format", DiagnosticSeverity.Warning, "Non-standard format",
                                    "Missing JPEG SOI marker. Preview may fail to render.");
                            }
                        }
                        else
                        {
                            report.AddCheck("Thumbnail Container", DiagnosticSeverity.Info, "Blank / Unset",
                                "Thumbnail is a valid blank container.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    report.AddCheck("Thumbnail Decryption", DiagnosticSeverity.Warning, "Decryption error", ex.Message);
                    report.CanRepairThumbnail = true;
                }
            }
            else
            {
                report.AddCheck("Thumbnail File", DiagnosticSeverity.Warning, "Missing",
                    $"{thumbFileName} does not exist in save directory. Coursebot displays a placeholder or uninitialized box.");
            }

            // Final Status Resolution
            if (!report.IsOccupiedInSave)
            {
                report.Status = SlotHealthStatus.HiddenInCoursebot;
                report.CanUnhide = true;
                report.PrimaryReason = "Course data is valid, but hidden in Coursebot because save.dat marks this slot as inactive (status = 0).";
            }
            else
            {
                report.Status = SlotHealthStatus.Healthy;
                report.PrimaryReason = "Course and slot registration are fully healthy.";
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
        /// Sanitizes invalid flags on all objects in a course slot, re-encrypts, and saves the file.
        /// </summary>
        public static bool SanitizeCourseFlags(string saveDir, int slotIndex)
        {
            var targets = SaveManagerService.GetTargetDirectories(saveDir);
            string fileName = $"course_data_{slotIndex:D3}.bcd";
            bool anySuccess = false;

            foreach (var dir in targets)
            {
                string coursePath = Path.Combine(dir, fileName);
                if (!File.Exists(coursePath)) continue;

                try
                {
                    byte[] rawBcd = File.ReadAllBytes(coursePath);
                    byte[] decrypted = LevelCrypto.DecryptLevel(rawBcd);

                    Level lvl = new Level();
                    lvl.LoadFromStream(new KaitaiStream(decrypted));

                    bool changed = false;
                    if (lvl.overworld?.objects != null)
                    {
                        foreach (var obj in lvl.overworld.objects)
                        {
                            uint sanitized = ActorCapabilities.SanitizeFlags(obj.id, obj.flag);
                            if (sanitized != obj.flag)
                            {
                                obj.flag = sanitized;
                                changed = true;
                            }
                        }
                    }

                    if (lvl.subworld?.objects != null)
                    {
                        foreach (var obj in lvl.subworld.objects)
                        {
                            uint sanitized = ActorCapabilities.SanitizeFlags(obj.id, obj.flag);
                            if (sanitized != obj.flag)
                            {
                                obj.flag = sanitized;
                                changed = true;
                            }
                        }
                    }

                    if (changed)
                    {
                        byte[] newDecrypted = lvl.GetBytes();
                        byte[] newEncrypted = LevelCrypto.EncryptLevel(newDecrypted);
                        File.WriteAllBytes(coursePath, newEncrypted);
                        anySuccess = true;
                    }
                    else
                    {
                        anySuccess = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to sanitize flags in {dir}: {ex.Message}");
                }
            }

            return anySuccess;
        }
    }
}
