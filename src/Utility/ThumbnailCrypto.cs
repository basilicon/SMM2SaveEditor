using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Drawing;
using System.Drawing.Imaging;

namespace SMM2SaveEditor.Utility
{
    /// <summary>
    /// Cryptographic helper for Super Mario Maker 2 course thumbnails (.btl).
    ///
    /// Authentic SMM2 .btl file layout:
    ///   - Total size: Exactly 0x1C000 bytes (114,688 bytes).
    ///   - No unencrypted header!
    ///   - 0x00000..0x1BFCE: 114,640 bytes AES-128-CBC encrypted body (0x1BFD0 bytes).
    ///     * The decrypted payload is a standard JFIF / JPEG directly starting at byte 0 (0xFF, 0xD8, 0xFF...).
    ///     * Followed by null padding up to 114,640 bytes.
    ///   - 0x1BFD0..0x1BFFF: 48 bytes crypto footer
    ///     * +0x00..+0x0F (16 bytes): AES IV (4x uint32: [s2, s1, s4, s3])
    ///     * +0x10..+0x1F (16 bytes): RNG Seed (4x uint32: [s1, s2, s3, s4])
    ///     * +0x20..+0x2F (16 bytes): AES-CMAC signature over the 114,640-byte decrypted body
    /// </summary>
    public static class ThumbnailCrypto
    {
        public const int BtlFileSize = 0x1C000;       // 114,688 bytes
        public const int FooterSize = 0x30;           // 48 bytes
        public const int BodySize = BtlFileSize - FooterSize; // 0x1BFD0 = 114,640 bytes
        public const int FooterOffset = BodySize;     // 0x1BFD0


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
        /// Decrypts a course_thumb_XXX.btl file and returns the decrypted image bytes (JPEG).
        /// Returns empty byte array if the file is an empty/uninitialized slot (all zeros).
        /// </summary>
        public static byte[] DecryptThumbnail(byte[] buf)
        {
            if (buf.Length != BtlFileSize)
            {
                throw new ArgumentException($"Invalid .btl buffer size: {buf.Length} (expected {BtlFileSize})");
            }

            int end = FooterOffset;

            Random r = new Random
            {
                v0 = BitConverter.ToUInt32(buf, end + 0x10),
                v1 = BitConverter.ToUInt32(buf, end + 0x14),
                v2 = BitConverter.ToUInt32(buf, end + 0x18),
                v3 = BitConverter.ToUInt32(buf, end + 0x1C),
            };

            byte[] cmacWant = buf.Skip(end + 0x20).Take(0x10).ToArray();

            // Construct AES key from KeyTables.Thumbnail
            MemoryStream aesKey = new MemoryStream();
            CreateKey(ref r, KeyTables.Thumbnail, 0x10, aesKey);

            using Aes aesBlock = Aes.Create();
            aesBlock.Key = aesKey.ToArray();
            aesBlock.Mode = CipherMode.CBC;
            aesBlock.Padding = PaddingMode.None;

            byte[] aesIv = buf.Skip(end).Take(0x10).ToArray();
            using ICryptoTransform aesDecrypt = aesBlock.CreateDecryptor(aesBlock.Key, aesIv);

            byte[] decrypted = new byte[BodySize];
            aesDecrypt.TransformBlock(buf, 0, BodySize, decrypted, 0);

            // CMAC check
            MemoryStream cmacKey = new MemoryStream();
            CreateKey(ref r, KeyTables.Thumbnail, 0x10, cmacKey);
            byte[] cmacDigest = AesCmac.Calc(decrypted, cmacKey.ToArray());

            if (!cmacDigest.SequenceEqual(cmacWant))
            {
                throw new Exception("Thumbnail decryption CMAC digest verification failed.");
            }

            // If empty slot (all zeros), return empty
            if (decrypted[0] == 0 && decrypted[1] == 0 && decrypted[2] == 0)
            {
                return Array.Empty<byte>();
            }

            // Extract valid JPEG payload (JPEG starts with FF D8 FF and ends with FF D9)
            int jpegLength = FindJpegLength(decrypted);
            if (jpegLength > 0 && jpegLength <= decrypted.Length)
            {
                byte[] jpeg = new byte[jpegLength];
                Buffer.BlockCopy(decrypted, 0, jpeg, 0, jpegLength);
                return jpeg;
            }

            return decrypted;
        }

        /// <summary>
        /// Encrypts raw image bytes (JPEG) into a valid Nintendo-signed course_thumb_XXX.btl file (0x1C000 bytes).
        /// </summary>
        public static byte[] EncryptThumbnail(byte[] rawImageBytes)
        {
            if (rawImageBytes.Length > 0x1BF9C)
            {
                throw new ArgumentException($"Image payload is too large ({rawImageBytes.Length} > {0x1BF9C} bytes).");
            }

            // Prepare 114,640-byte buffer with JPEG data followed by 0x00 padding
            byte[] body = new byte[BodySize];
            Buffer.BlockCopy(rawImageBytes, 0, body, 0, rawImageBytes.Length);

            // SMM2 thumbnail container size tag at 0x1BF9C (114,588 bytes = 0x1BF9C)
            Buffer.BlockCopy(BitConverter.GetBytes(0x0001BF9C), 0, body, 0x1BF9C, 4);

            // 1. Generate random 16-byte inner seed for HMAC-SHA256 at 0x1BFC0
            byte[] innerSeed = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(innerSeed);
            }
            Buffer.BlockCopy(innerSeed, 0, body, 0x1BFC0, 16);

            Random innerR = new Random
            {
                v0 = BitConverter.ToUInt32(innerSeed, 0),
                v1 = BitConverter.ToUInt32(innerSeed, 4),
                v2 = BitConverter.ToUInt32(innerSeed, 8),
                v3 = BitConverter.ToUInt32(innerSeed, 12),
            };

            // 2. Derive 16-byte HMAC key from inner seed using KeyTables.Thumbnail
            MemoryStream hmacKeyStream = new MemoryStream();
            CreateKey(ref innerR, KeyTables.Thumbnail, 0x10, hmacKeyStream);
            byte[] hmacKey = hmacKeyStream.ToArray();

            // 3. Compute HMAC-SHA256 over body[0..0x1BF9C] (114,588 bytes) and store 32-byte digest at 0x1BFA0
            using (var hmac = new HMACSHA256(hmacKey))
            {
                byte[] hmacDigest = hmac.ComputeHash(body, 0, 0x1BF9C);
                Buffer.BlockCopy(hmacDigest, 0, body, 0x1BFA0, 32);
            }

            // Generate random 16-byte seed
            byte[] randomSeed = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomSeed);
            }

            uint s1 = BitConverter.ToUInt32(randomSeed, 0);
            uint s2 = BitConverter.ToUInt32(randomSeed, 4);
            uint s3 = BitConverter.ToUInt32(randomSeed, 8);
            uint s4 = BitConverter.ToUInt32(randomSeed, 12);

            // Nintendo's ENL IV rule: pair-swapped seed [s2, s1, s4, s3]
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
            CreateKey(ref r, KeyTables.Thumbnail, 0x10, aesKey);

            using Aes aesBlock = Aes.Create();
            aesBlock.Key = aesKey.ToArray();
            aesBlock.Mode = CipherMode.CBC;
            aesBlock.Padding = PaddingMode.None;

            using ICryptoTransform aesEncrypt = aesBlock.CreateEncryptor(aesBlock.Key, aesIv);

            byte[] encrypted = new byte[BodySize];
            aesEncrypt.TransformBlock(body, 0, body.Length, encrypted, 0);

            // Compute CMAC signature over decrypted body
            MemoryStream cmacKey = new MemoryStream();
            CreateKey(ref r, KeyTables.Thumbnail, 0x10, cmacKey);
            byte[] cmacDigest = AesCmac.Calc(body, cmacKey.ToArray());

            // Write encrypted body (114,640 bytes) and 48-byte footer (16 bytes IV + 16 bytes Seed + 16 bytes CMAC)
            MemoryStream writer = new MemoryStream(BtlFileSize);
            writer.Write(encrypted, 0, encrypted.Length);
            writer.Write(aesIv, 0, aesIv.Length);
            writer.Write(randomSeed, 0, randomSeed.Length);
            writer.Write(cmacDigest, 0, cmacDigest.Length);

            byte[] outArr = writer.ToArray();
            if (outArr.Length != BtlFileSize)
            {
                throw new InvalidOperationException($"Encryption produced invalid file size: {outArr.Length} != {BtlFileSize}");
            }

            return outArr;
        }

        /// <summary>
        /// Checks whether a JPEG has the exact dimensions (640x360) and standard fixed IJG Huffman tables
        /// (lengths 31, 181, 31, 181) required by Nintendo Switch's NVJPEG hardware decoder.
        /// </summary>
        public static bool IsCompliantJpeg(byte[] data)
        {
            if (data == null || data.Length < 4 || data.Length > 0x1BF9C)
                return false;
            if (data[0] != 0xFF || data[1] != 0xD8)
                return false;

            bool hasValidSof0 = false;
            int dhtCount = 0;
            bool dhtStandard = true;

            int i = 2;
            while (i < data.Length - 1)
            {
                if (data[i] == 0xFF && data[i + 1] != 0 && data[i + 1] != 0xFF)
                {
                    byte m = data[i + 1];
                    if (m == 0xDA) // SOS reached
                    {
                        break;
                    }
                    if (m == 0xD8 || m == 0xD9)
                    {
                        i += 2;
                        continue;
                    }
                    if (i + 4 > data.Length) break;
                    int len = (data[i + 2] << 8) | data[i + 3];
                    if (m == 0xC0) // SOF0
                    {
                        if (len >= 9 && i + 2 + len <= data.Length)
                        {
                            int height = (data[i + 5] << 8) | data[i + 6];
                            int width = (data[i + 7] << 8) | data[i + 8];
                            if (width == 640 && height == 360)
                            {
                                hasValidSof0 = true;
                            }
                        }
                    }
                    else if (m == 0xC4) // DHT
                    {
                        dhtCount++;
                        // Standard fixed IJG DHTs are 31 bytes (DC) and 181 bytes (AC)
                        if (len != 31 && len != 181)
                        {
                            dhtStandard = false;
                        }
                    }
                    i += 2 + len;
                }
                else
                {
                    i++;
                }
            }

            return hasValidSof0 && dhtStandard && dhtCount == 4;
        }

        /// <summary>
        /// Resizes and encodes any image to 640x360 baseline JPEG with standard fixed IJG Huffman tables
        /// using System.Drawing (GDI+), ensuring compatibility with Nintendo's NVJPEG hardware decoder.
        /// </summary>
        public static byte[] EncodeStandardJpeg(byte[] inputBytes)
        {
            using var originalBitmap = SkiaSharp.SKBitmap.Decode(inputBytes);
            if (originalBitmap == null)
            {
                throw new ArgumentException("Unable to decode the provided image file.");
            }

            const int targetWidth = 640;
            const int targetHeight = 360;

            using var resized = new SkiaSharp.SKBitmap(targetWidth, targetHeight, SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul);
            using (var canvas = new SkiaSharp.SKCanvas(resized))
            {
                canvas.Clear(SkiaSharp.SKColors.Black);

                float srcAspect = (float)originalBitmap.Width / originalBitmap.Height;
                float dstAspect = (float)targetWidth / targetHeight;

                SkiaSharp.SKRect destRect;
                if (srcAspect > dstAspect)
                {
                    float scale = (float)targetHeight / originalBitmap.Height;
                    float w = originalBitmap.Width * scale;
                    float x = (targetWidth - w) / 2f;
                    destRect = new SkiaSharp.SKRect(x, 0, x + w, targetHeight);
                }
                else
                {
                    float scale = (float)targetWidth / originalBitmap.Width;
                    float h = originalBitmap.Height * scale;
                    float y = (targetHeight - h) / 2f;
                    destRect = new SkiaSharp.SKRect(0, y, targetWidth, y + h);
                }

                using var paint = new SkiaSharp.SKPaint
                {
                    FilterQuality = SkiaSharp.SKFilterQuality.High,
                    IsAntialias = true
                };

                canvas.DrawBitmap(originalBitmap, destRect, paint);
            }

            using var gdiBitmap = new Bitmap(targetWidth, targetHeight, PixelFormat.Format32bppArgb);
            var bmpData = gdiBitmap.LockBits(
                new Rectangle(0, 0, targetWidth, targetHeight),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                IntPtr srcPixels = resized.GetPixels();
                int byteCount = targetWidth * targetHeight * 4;
                unsafe
                {
                    Buffer.MemoryCopy((void*)srcPixels, (void*)bmpData.Scan0, byteCount, byteCount);
                }
            }
            finally
            {
                gdiBitmap.UnlockBits(bmpData);
            }

            ImageCodecInfo? jpegCodec = null;
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
            {
                if (codec.FormatID == ImageFormat.Jpeg.Guid)
                {
                    jpegCodec = codec;
                    break;
                }
            }

            if (jpegCodec == null)
            {
                throw new InvalidOperationException("JPEG encoder not found in System.Drawing.");
            }

            long quality = 80L;
            while (quality >= 30L)
            {
                using var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);

                using var ms = new MemoryStream();
                gdiBitmap.Save(ms, jpegCodec, encoderParams);
                byte[] encoded = ms.ToArray();

                if (encoded.Length <= 0x1BF9C)
                {
                    return encoded;
                }

                quality -= 10L;
            }

            throw new InvalidOperationException("Could not compress thumbnail to fit within 114,588 bytes.");
        }

        /// <summary>
        /// Takes any image stream (PNG, JPEG, WebP, BMP, etc.), ensures 640x360 dimensions,
        /// formats/encodes with standard fixed IJG Huffman tables required by Nintendo's NVJPEG decoder,
        /// and encrypts into a valid .btl file.
        /// </summary>
        public static byte[] ConvertImageToBtl(Stream imageStream)
        {
            byte[]? rawInput = null;
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }
            using (var ms = new MemoryStream())
            {
                imageStream.CopyTo(ms);
                rawInput = ms.ToArray();
            }

            // Direct passthrough if already a compliant JPEG (e.g. authentic dumped SMM2 thumbnail)
            if (rawInput != null && IsCompliantJpeg(rawInput))
            {
                return EncryptThumbnail(rawInput);
            }

            // Otherwise re-encode to 640x360 standard fixed IJG JPEG
            byte[] jpegBytes = EncodeStandardJpeg(rawInput!);
            return EncryptThumbnail(jpegBytes);
        }

        private static int FindJpegLength(byte[] data)
        {
            // JPEG begins with 0xFF 0xD8 and ends with 0xFF 0xD9
            if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8)
            {
                return data.Length;
            }

            for (int i = 2; i < data.Length - 1; i++)
            {
                if (data[i] == 0xFF)
                {
                    byte next = data[i + 1];
                    if (next == 0xD9) // EOI (End of Image)
                    {
                        return i + 2;
                    }
                    if (next == 0x00) // Byte stuffing inside entropy scan
                    {
                        i++;
                    }
                }
            }

            return data.Length;
        }
    }
}
