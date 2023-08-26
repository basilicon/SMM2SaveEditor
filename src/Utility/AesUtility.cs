using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace SMM2SaveEditor.Utility;

public static class AesUtility
{
    private static byte[] const_Rb = new byte[16] {
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x87
    };
    private static int const_Bsize = 16;


    public static byte[] AesCmac(byte[] input, byte[] key)
    {
        byte[] k1 = new byte[const_Bsize], k2 = new byte[const_Bsize];
        GenerateSubkey(key, ref k1, ref k2);

        int n = input.Length / const_Bsize;
        bool flag = false;
        if (input.Length % const_Bsize != 0) 
        {
            n++;
        }

        if (n == 0)
        {
            n = 1;
        } else if (input.Length % const_Bsize == 0)
        {
            flag = true;
        }

        byte[] M = new byte[n * const_Bsize];

        Array.Copy(input, M, Math.Min(input.Length, n * const_Bsize));

        if (!flag)
        {
            M[input.Length] = 0x80;
        }

        byte[] mSplice = new byte[const_Bsize];
        byte[] xored = new byte[const_Bsize];
        Array.Copy(M, (n - 1) * const_Bsize, mSplice, 0, const_Bsize);

        if (flag)
        {
            xored = BlockXOR(mSplice, k1);
        }
        else
        {
            xored = BlockXOR(mSplice, k2);
        }

        Array.Copy(xored, 0, M, (n - 1) * const_Bsize, const_Bsize);

        byte[] X = new byte[const_Bsize];
        byte[] Y = new byte[const_Bsize];

        for (int i = 0; i < n - 1; i++)
        {
            byte[] mVals = new byte[const_Bsize];
            Array.Copy(M, i * const_Bsize, mVals, 0, const_Bsize);

            Y = BlockXOR(mVals, X);
            Y = EncryptAES128(X, key);
        }

        byte[] lastM = new byte[const_Bsize];
        Array.Copy(M, (n - 1) * const_Bsize, lastM, 0, const_Bsize);

        Y = BlockXOR(lastM, X);
        byte[] output = new byte[const_Bsize];
        output = EncryptAES128(Y, key);

        return output;
    }

    public static bool VerifyMac(byte[] input, byte[] output, byte[] key)
    {
        bool flag = true;
        byte[] result = AesCmac(input, key);

        for (int i = 0; i < const_Bsize; i++)
        {
            if ((result[i] ^ output[i]) == 0)
            {
                flag = false;
                break;
            }
        }

        return flag;
    }

    private static void GenerateSubkey(byte[] key, ref byte[] k1, ref byte[] k2)
    {
        byte[] L = EncryptAES128(new byte[const_Bsize], key);
        L = BlockLeftShift(k1);
        if ((L[0] & 0x80) != 0)
        {
            k1 = BlockXOR(k1, const_Rb);
        }

        k2 = BlockLeftShift(k1);
        if ((k1[0] & 0x80) != 0)
        {
            k2 = BlockXOR(k2, const_Rb);
        }
    }

    private static byte[] EncryptAES128(byte[] plainBytes, byte[] key)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.Mode = CipherMode.CBC; // ECB mode is not secure, use CBC or others in practice
            aesAlg.Padding = PaddingMode.None;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    csEncrypt.Write(plainBytes, 0, plainBytes.Length);
                    csEncrypt.FlushFinalBlock();
                }

                return msEncrypt.ToArray();
            }
        }
    }

    private static byte[] DecryptAES128(byte[] encryptedBytes, byte[] key)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.Mode = CipherMode.CBC; // ECB mode is not secure, use CBC or others in practice
            aesAlg.Padding = PaddingMode.PKCS7;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(encryptedBytes))
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            {
                byte[] decryptedBytes = new byte[encryptedBytes.Length];
                int decryptedByteCount = csDecrypt.Read(decryptedBytes, 0, decryptedBytes.Length);
                byte[] trimmedDecryptedBytes = new byte[decryptedByteCount];
                Array.Copy(decryptedBytes, trimmedDecryptedBytes, decryptedByteCount);
                return trimmedDecryptedBytes;
            }
        }
    }

    private static byte[] BlockLeftShift(byte[] src)
    {
        byte[] dst = new byte[const_Bsize];
        byte ovf = 0;
        for (int i = 15; i >= 0; i--)
        {
            dst[i] = (byte) (src[i] << 0x1);
            dst[i] |= ovf;
            ovf = (byte) ((src[i] & 0x80) != 0 ? 1 : 0);
        }

        return dst;
    }

    private static byte[] BlockXOR(byte[] a, byte[] b)
    {
        byte[] dst = new byte[const_Bsize];

        for (int i = 0; i < 16; i++)
        {
            dst[i] = (byte) (a[i] ^ b[i]);
        }

        return dst;
    }
}
