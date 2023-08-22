using System;
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

        private static uint[] bcdTable =
        {
        0x7ab1c9d2, 0xca750936, 0x3003e59c, 0xf261014b,
        0x2e25160a, 0xed614811, 0xf1ac6240, 0xd59272cd,
        0xf38549bf, 0x6cf5b327, 0xda4db82a, 0x820c435a,
        0xc95609ba, 0x19be08b0, 0x738e2b81, 0xed3c349a,
        0x045275d1, 0xe0a73635, 0x1debf4da, 0x9924b0de,
        0x6a1fc367, 0x71970467, 0xfc55abeb, 0x368d7489,
        0x0cc97d1d, 0x17cc441e, 0x3528d152, 0xd0129b53,
        0xe12a69e9, 0x13d1bdb7, 0x32eaa9ed, 0x42f41d1b,
        0xaea5f51f, 0x42c5d23c, 0x7cc742ed, 0x723ba5f9,
        0xde5b99e3, 0x2c0055a4, 0xc38807b4, 0x4c099b61,
        0xc4e4568e, 0x8c29c901, 0xe13b34ac, 0xe7c3f212,
        0xb67ef941, 0x08038965, 0x8afd1e6a, 0x8e5341a3,
        0xa4c61107, 0xfbaf1418, 0x9b05ef64, 0x3c91734e,
        0x82ec6646, 0xfb19f33e, 0x3bde6fe2, 0x17a84cca,
        0xccdf0ce9, 0x50e4135c, 0xff2658b2, 0x3780f156,
        0x7d8f5d68, 0x517cbed1, 0x1fcddf0d, 0x77a58c94,
    };

        private static void CreateKey(Random random, uint[] bcdTable, int size, MemoryStream keyStream)
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
                throw new ArgumentException(string.Format("invalid buf size {0} != {1}", buf.Length, 0x5c000));
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

            //Debug.Log(String.Format("{0} {1} {2} {3}", r.v0, r.v1, r.v2, r.v3));

            byte[] cmacWant = buf.Skip(end + 0x20).Take(0x10).ToArray();
            byte[] crcWant = buf.Skip(8).Take(4).ToArray();

            // Construct AES instance
            MemoryStream aesKey = new MemoryStream();
            CreateKey(r, bcdTable, 0x10, aesKey);

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
                throw new Exception("crc invalid");
            }

            // WHO NEEDS THE CMAC CHECK LMFAO
            //// cmac check
            //MemoryStream cmacKey = new MemoryStream();
            //CreateKey(r, bcdTable, 0x10, cmacKey);

            //Aes cmacBlock = Aes.Create();
            //cmacBlock.Key = cmacKey.ToArray();

            //byte[] cmacDigest;
            //using (var ms = new MemoryStream(decrypted))
            //using (var cryptoStream = new CryptoStream(ms, cmacBlock.CreateDecryptor(), CryptoStreamMode.Read))
            //{
            //    cmacDigest = new byte[0x10];
            //    int bytesRead = cryptoStream.Read(cmacDigest, 0, 0x10);
            //    if (bytesRead != 0x10)
            //    {
            //        throw new Exception("cmac invalid");
            //    }
            //}

            //if (!cmacDigest.SequenceEqual(cmacWant))
            //{
            //    throw new Exception("cmac invalid");
            //}

            //if (false)
            //{
            //    // write bcd header
            //    writer.Write(buf, 0, 0x10);
            //}

            // Decrypted course data
            writer.Write(decrypted, 0, decrypted.Length);

            return writer.ToArray();
        }

        public static byte[] EncryptLevel(byte[] buf)
        {
            bool withoutBcdHeader = buf.Length == 0x5BFD0 - 0x10;

            if (!withoutBcdHeader && buf.Length != 0x5BFD0)
            {
                throw new ArgumentException($"invalid buf size {buf.Length} != {0x5BFD0}");
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
                writer.Write(BitConverter.GetBytes((uint)1), 0, sizeof(uint)); // 0x1
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

            // Technically random bytes, can make deterministic here
            byte[] randomSeed = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            Random r = new Random
            {
                v0 = BitConverter.ToUInt32(randomSeed, 0),
                v1 = BitConverter.ToUInt32(randomSeed, 4),
                v2 = BitConverter.ToUInt32(randomSeed, 8),
                v3 = BitConverter.ToUInt32(randomSeed, 12),
            };
            byte[] aesIv = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

            MemoryStream aesKey = new MemoryStream();
            CreateKey(r, bcdTable, 0x10, aesKey);

            Aes aesBlock = Aes.Create();
            aesBlock.Key = aesKey.ToArray();

            aesBlock.Mode = CipherMode.CBC;
            aesBlock.Padding = PaddingMode.None;
            ICryptoTransform aesMode = aesBlock.CreateEncryptor(aesBlock.Key, aesIv);

            byte[] encrypted = new byte[0x5BFC0];
            aesMode.TransformBlock(decrypted, 0, decrypted.Length, encrypted, 0);

            MemoryStream cmacKey = new MemoryStream();
            CreateKey(r, bcdTable, 0x10, cmacKey);

            Aes cmacBlock = Aes.Create();
            cmacBlock.Key = cmacKey.ToArray();

            byte[] cmacDigest;
            using (var ms = new MemoryStream(decrypted))
            using (var cryptoStream = new CryptoStream(ms, cmacBlock.CreateEncryptor(), CryptoStreamMode.Read))
            {
                cmacDigest = new byte[0x10];
                int bytesRead = cryptoStream.Read(cmacDigest, 0, 0x10);
                if (bytesRead != 0x10)
                {
                    throw new Exception("Failed to calculate CMAC digest.");
                }
            }

            writer.Write(encrypted, 0, encrypted.Length);
            writer.Write(aesIv, 0, aesIv.Length);
            writer.Write(randomSeed, 0, randomSeed.Length);
            writer.Write(cmacDigest, 0, cmacDigest.Length);

            return writer.ToArray();
        }
    }
}