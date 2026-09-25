using System;
using System.IO;
using System.Security.Cryptography;

namespace SMM2SaveEditor.Utility
{
    /// <summary>
    /// Cryptographic helper for Super Mario Maker 2 save.dat file.
    ///
    /// Authentic save.dat structure (Total 0xC000 = 49,152 bytes):
    ///   - 0x00000..0x0000F (16 bytes): Unencrypted Header
    ///     * +0x00..+0x07: Format/filetype header (0x01, 0x00, 0x00, 0x00, 0x0B, 0x00, 0x00, 0x00)
    ///     * +0x08..+0x0B (4 bytes): CRC32 of decrypted body prefix [0x0000..0xB910]
    ///     * +0x0C..+0x0F: Reserved (zeros)
    ///   - 0x00010..0x0BFCE (0xBFC0 = 49,088 bytes): AES-128-CBC Encrypted Body
    ///     * 0x0000..0xB910: Profile, story mode, outfits, settings
    ///     * 0xB910..0xB91F (16 bytes): Coursebot Section Header
    ///       - 0xB918..0xB91B: CRC32 of Coursebot Section [0xB920..0xBFC0]
    ///     * 0xB920..0xBAFF (60 slots * 8 bytes): Own Coursebot Slots
    ///       - Each slot: [slot_index (byte), status (byte), 0, 0, 0, 0, 0, 0]
    ///         * status = 1: OCCUPIED (active course)
    ///         * status = 0: AVAILABLE (empty)
    ///   - 0x0BFD0..0x0BFFF (48 bytes): Crypto Footer
    ///     * +0x00..+0x0F: AES IV (16 bytes)
    ///     * +0x10..+0x1F: RNG Seed (16 bytes)
    ///     * +0x20..+0x2F: AES-CMAC over 49,088-byte decrypted body
    /// </summary>
    public static class SaveDataCrypto
    {
        public const int SaveFileSize = 0xC000;         // 49,152 bytes
        public const int HeaderSize = 0x10;             // 16 bytes
        public const int FooterSize = 0x30;             // 48 bytes
        public const int BodySize = 0xBFC0;             // 49,088 bytes
        public const int FooterOffset = HeaderSize + BodySize; // 0xBFD0

        public const int CoursebotCrcOffset = 0xB918;   // in decrypted body
        public const int CoursebotSlotsOffset = 0xB920; // in decrypted body
        public const int MaxCoursebotSlots = 60;

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

        /// <summary>
        /// Decrypts a save.dat file and returns (header, decryptedBody).
        /// </summary>
        public static (byte[] Header, byte[] Body) DecryptSave(byte[] saveDat)
        {
            if (saveDat.Length != SaveFileSize)
            {
                throw new ArgumentException($"Invalid save.dat size: {saveDat.Length} (expected {SaveFileSize})");
            }

            byte[] header = new byte[HeaderSize];
            Buffer.BlockCopy(saveDat, 0, header, 0, HeaderSize);

            int end = FooterOffset;
            Random r = new Random
            {
                v0 = BitConverter.ToUInt32(saveDat, end + 0x10),
                v1 = BitConverter.ToUInt32(saveDat, end + 0x14),
                v2 = BitConverter.ToUInt32(saveDat, end + 0x18),
                v3 = BitConverter.ToUInt32(saveDat, end + 0x1C),
            };

            byte[] cmacWant = new byte[16];
            Buffer.BlockCopy(saveDat, end + 0x20, cmacWant, 0, 16);

            MemoryStream aesKey = new MemoryStream();
            CreateKey(ref r, KeyTables.Save, 0x10, aesKey);

            using Aes aesBlock = Aes.Create();
            aesBlock.Key = aesKey.ToArray();
            aesBlock.Mode = CipherMode.CBC;
            aesBlock.Padding = PaddingMode.None;

            byte[] aesIv = new byte[16];
            Buffer.BlockCopy(saveDat, end, aesIv, 0, 16);

            using ICryptoTransform aesDecrypt = aesBlock.CreateDecryptor(aesBlock.Key, aesIv);
            byte[] decrypted = new byte[BodySize];
            aesDecrypt.TransformBlock(saveDat, HeaderSize, BodySize, decrypted, 0);

            // Verify CMAC
            MemoryStream cmacKey = new MemoryStream();
            CreateKey(ref r, KeyTables.Save, 0x10, cmacKey);
            byte[] cmacDigest = AesCmac.Calc(decrypted, cmacKey.ToArray());

            for (int i = 0; i < 16; i++)
            {
                if (cmacDigest[i] != cmacWant[i])
                {
                    throw new Exception("save.dat CMAC verification failed.");
                }
            }

            return (header, decrypted);
        }

        /// <summary>
        /// Recalculates CRCs, encrypts the save.dat body, computes CMAC, and returns full 49,152-byte save.dat.
        /// </summary>
        public static byte[] EncryptSave(byte[] header, byte[] decryptedBody)
        {
            if (header.Length != HeaderSize || decryptedBody.Length != BodySize)
            {
                throw new ArgumentException("Invalid header or body size for save.dat encryption.");
            }

            byte[] body = (byte[])decryptedBody.Clone();
            byte[] hdr = (byte[])header.Clone();
            Crc32 crc = new Crc32();

            // 1. Recalculate Coursebot Section CRC (from 0xB920 to 0xBFC0)
            byte[] cbData = new byte[BodySize - CoursebotSlotsOffset];
            Buffer.BlockCopy(body, CoursebotSlotsOffset, cbData, 0, cbData.Length);
            uint cbCrc = crc.ComputeChecksum(cbData);
            Buffer.BlockCopy(BitConverter.GetBytes(cbCrc), 0, body, CoursebotCrcOffset, 4);

            // 2. Recalculate Main Save Body Prefix CRC (from 0x0000 to 0xB910)
            byte[] mainPrefix = new byte[0xB910];
            Buffer.BlockCopy(body, 0, mainPrefix, 0, mainPrefix.Length);
            uint mainCrc = crc.ComputeChecksum(mainPrefix);
            Buffer.BlockCopy(BitConverter.GetBytes(mainCrc), 0, hdr, 8, 4);

            // 3. Generate Random Seed and IV
            byte[] randomSeed = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomSeed);
            }

            uint s1 = BitConverter.ToUInt32(randomSeed, 0);
            uint s2 = BitConverter.ToUInt32(randomSeed, 4);
            uint s3 = BitConverter.ToUInt32(randomSeed, 8);
            uint s4 = BitConverter.ToUInt32(randomSeed, 12);

            byte[] aesIv = new byte[16];
            Buffer.BlockCopy(BitConverter.GetBytes(s2), 0, aesIv, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(s1), 0, aesIv, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(s4), 0, aesIv, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(s3), 0, aesIv, 12, 4);

            Random r = new Random { v0 = s1, v1 = s2, v2 = s3, v3 = s4 };

            MemoryStream aesKey = new MemoryStream();
            CreateKey(ref r, KeyTables.Save, 0x10, aesKey);

            using Aes aesBlock = Aes.Create();
            aesBlock.Key = aesKey.ToArray();
            aesBlock.Mode = CipherMode.CBC;
            aesBlock.Padding = PaddingMode.None;

            using ICryptoTransform aesEncrypt = aesBlock.CreateEncryptor(aesBlock.Key, aesIv);
            byte[] encrypted = new byte[BodySize];
            aesEncrypt.TransformBlock(body, 0, BodySize, encrypted, 0);

            // 4. Compute CMAC signature over decrypted body
            MemoryStream cmacKey = new MemoryStream();
            CreateKey(ref r, KeyTables.Save, 0x10, cmacKey);
            byte[] cmacDigest = AesCmac.Calc(body, cmacKey.ToArray());

            // 5. Assemble final save.dat
            MemoryStream writer = new MemoryStream(SaveFileSize);
            writer.Write(hdr, 0, hdr.Length);
            writer.Write(encrypted, 0, encrypted.Length);
            writer.Write(aesIv, 0, aesIv.Length);
            writer.Write(randomSeed, 0, randomSeed.Length);
            writer.Write(cmacDigest, 0, cmacDigest.Length);

            return writer.ToArray();
        }

        /// <summary>
        /// Updates a slot's registration status in save.dat (1 = OCCUPIED, 0 = AVAILABLE).
        /// Synchronizes changes across both save buffer directories (0 and 1).
        /// </summary>
        public static bool SetSlotStatus(string saveDir, int slotIndex, bool occupied)
        {
            if (slotIndex < 0 || slotIndex >= MaxCoursebotSlots) return false;

            var targets = SaveManagerService.GetTargetDirectories(saveDir);
            bool anyUpdated = false;

            foreach (var target in targets)
            {
                string saveDatPath = Path.Combine(target, "save.dat");
                if (!File.Exists(saveDatPath)) continue;

                try
                {
                    byte[] raw = File.ReadAllBytes(saveDatPath);
                    var (header, body) = DecryptSave(raw);

                    int slotOffset = CoursebotSlotsOffset + (slotIndex * 8);
                    byte newStatus = occupied ? (byte)1 : (byte)0;

                    if (body[slotOffset + 1] != newStatus)
                    {
                        body[slotOffset] = (byte)slotIndex;
                        body[slotOffset + 1] = newStatus;

                        byte[] newSaveDat = EncryptSave(header, body);
                        File.WriteAllBytes(saveDatPath, newSaveDat);
                        anyUpdated = true;
                    }
                    else
                    {
                        anyUpdated = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SaveDataCrypto] Error updating save.dat in {target}: {ex.Message}");
                }
            }

            return anyUpdated;
        }

        /// <summary>
        /// Reads the registration status of a Coursebot slot from save.dat (true = OCCUPIED, false = AVAILABLE).
        /// </summary>
        public static bool GetSlotStatus(string saveDir, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxCoursebotSlots) return false;

            var targets = SaveManagerService.GetTargetDirectories(saveDir);
            foreach (var target in targets)
            {
                string saveDatPath = Path.Combine(target, "save.dat");
                if (!File.Exists(saveDatPath)) continue;

                try
                {
                    byte[] raw = File.ReadAllBytes(saveDatPath);
                    var (_, body) = DecryptSave(raw);
                    int slotOffset = CoursebotSlotsOffset + (slotIndex * 8);
                    return body[slotOffset + 1] == 1;
                }
                catch
                {
                    // Continue to next directory
                }
            }

            return false;
        }
    }
}
