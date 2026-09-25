using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace SMM2SaveEditor.Utility
{

    public class LevelCrypto
    {
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

        private static uint[] bcdTable => KeyTables.Course;

        private static void CreateKey(ref Random random, uint[] bcdTable, int size, MemoryStream keyStream)
        {
            for (uint i = 0; i < size / 4; i++)
            {
                uint value = 0;
                for (int e = 0; e < 4; e++)
                {
                    uint index = random.NextUInt((uint)bcdTable.Count());
                    uint shift = random.NextUInt(4) * 8;
                    uint b = (bcdTable[(int)index] >> (int)shift) & 0xFF;
                    value = (value << 8) | b;
                }

                keyStream.Write(BitConverter.GetBytes(value), 0, sizeof(uint));
            }
        }
        private static uint Crc32Calculate(byte[] data)
        {
            Crc32 crc = new();

            return crc.ComputeChecksum(data);
        }

        public static byte[] DecryptLevel(byte[] buf)
        {
            if (buf.Length != 0x5c000)
            {
                throw new ArgumentException($"Invalid buffer size {buf.Length} != {0x5c000}");
            }

            int end = 0x5BFD0;
            MemoryStream writer = new MemoryStream();


            // Create random instance
            Random r = new Random
            {
                v0 = BitConverter.ToUInt32(buf, end + 0x10),
                v1 = BitConverter.ToUInt32(buf, end + 0x14),
                v2 = BitConverter.ToUInt32(buf, end + 0x18),
                v3 = BitConverter.ToUInt32(buf, end + 0x1C),
            };

            byte[] cmacWant = buf.Skip(end + 0x20).Take(0x10).ToArray();
            byte[] crcWant = buf.Skip(8).Take(4).ToArray();

            // Construct AES instance
            MemoryStream aesKey = new MemoryStream();
            CreateKey(ref r, bcdTable, 0x10, aesKey);

            Aes aesBlock = Aes.Create();
            aesBlock.Key = aesKey.ToArray();

            aesBlock.Mode = CipherMode.CBC;
            aesBlock.Padding = PaddingMode.None;
            ICryptoTransform aesMode = aesBlock.CreateDecryptor(aesBlock.Key, buf.Skip(end).Take(0x10).ToArray());

            byte[] decrypted = new byte[0x5BFC0];
            aesMode.TransformBlock(buf, 0x10, 0x5BFD0 - 0x10, decrypted, 0);

            // crc check
            if (Crc32Calculate(decrypted) != BitConverter.ToUInt32(crcWant))
            {
                throw new Exception("Decryption error: CRC failed.");
            }

            // cmac check
            MemoryStream cmacKey = new MemoryStream();
            CreateKey(ref r, bcdTable, 0x10, cmacKey);

            byte[] cmacDigest = AesCmac.Calc(decrypted, cmacKey.ToArray());

            if (!cmacDigest.SequenceEqual(cmacWant))
            {
                throw new Exception("Decryption error: CMAC digest is invalid.");
            }

            // Decrypted course data
            writer.Write(decrypted, 0, decrypted.Length);

            return writer.ToArray();
        }


        public static byte[] EncryptLevel(byte[] buf)
        {
            bool withoutBcdHeader = buf.Length == 0x5BFD0 - 0x10;

            if (!withoutBcdHeader && buf.Length != 0x5BFD0)
            {
                throw new ArgumentException($"invalid buf size {buf.Length} != {0x5BFD0} (difference of {buf.Length - 0x5BFD0})");
            }

            MemoryStream writer = new MemoryStream();

            var reader = withoutBcdHeader ? new MemoryStream(buf) : new MemoryStream(buf, 0x10, buf.Length - 0x10);

            byte[] decrypted = new byte[0x5BFC0];
            if (reader.Read(decrypted, 0, decrypted.Length) != decrypted.Length)
            {
                throw new IOException("Failed to read data from the reader.");
            }

            if (withoutBcdHeader)
            {
                uint crc = Crc32Calculate(decrypted);

                // Header data
                writer.Write(BitConverter.GetBytes((uint)0x1), 0, sizeof(uint));
                writer.Write(BitConverter.GetBytes((ushort)0x10), 0, sizeof(ushort));
                writer.Write(BitConverter.GetBytes((ushort)0x0), 0, sizeof(ushort));
                writer.Write(BitConverter.GetBytes(crc), 0, sizeof(uint));
                writer.WriteByte(0x53);
                writer.WriteByte(0x43);
                writer.WriteByte(0x44);
                writer.WriteByte(0x4C);
            }
            else
            {
                // Update the CRC value in the existing header
                uint crc = Crc32Calculate(decrypted);
                BitConverter.GetBytes(crc).CopyTo(buf, 0x8);
                writer.Write(buf, 0, 0x10);
            }

            // Generate genuine 16-byte random seed and Nintendo ENL pair-swapped IV: [s2, s1, s4, s3]
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

            Random r = new Random
            {
                v0 = s1,
                v1 = s2,
                v2 = s3,
                v3 = s4,
            };

            MemoryStream aesKey = new MemoryStream();
            CreateKey(ref r, bcdTable, 0x10, aesKey);

            Aes aesBlock = Aes.Create();
            aesBlock.Key = aesKey.ToArray();

            aesBlock.Mode = CipherMode.CBC;
            aesBlock.Padding = PaddingMode.None;
            ICryptoTransform aesMode = aesBlock.CreateEncryptor(aesBlock.Key, aesIv);

            byte[] encrypted = new byte[0x5BFC0];
            aesMode.TransformBlock(decrypted, 0, decrypted.Length, encrypted, 0);

            MemoryStream cmacKey = new MemoryStream();
            CreateKey(ref r, bcdTable, 0x10, cmacKey);

            byte[] cmacDigest = AesCmac.Calc(decrypted, cmacKey.ToArray());

            writer.Write(encrypted, 0, encrypted.Length);
            writer.Write(aesIv, 0, aesIv.Length);
            writer.Write(randomSeed, 0, randomSeed.Length);
            writer.Write(cmacDigest, 0, cmacDigest.Length);

            byte[] outArr = writer.ToArray();

            if (outArr.Length != 0x5c000)
            {
                throw new ArgumentException($"Encryption failed: Invalid buffer size {outArr.Length} != {0x5c000}");
            }

            return outArr;
        }
    }
}