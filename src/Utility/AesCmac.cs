using System;
using System.Diagnostics;

using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;

namespace SMM2SaveEditor.Utility;

public class AesCmac
{
    private readonly CMac Impl = new(new AesEngine());

    public AesCmac(byte[] key)
    {
        Impl.Init(new KeyParameter(key));
    }

    public void Update(byte[] data)
    {
        Impl.BlockUpdate(data, 0, data.Length);
    }

    public byte[] GetDigest()
    {
        byte[] digest = new byte[Impl.GetMacSize()];
        Impl.DoFinal(digest, 0);
        return digest;
    }

    public static byte[] Calc(byte[] data, byte[] key)
    {
        AesCmac cmac = new AesCmac(key);
        cmac.Update(data);
        return cmac.GetDigest();
    }
    
}
