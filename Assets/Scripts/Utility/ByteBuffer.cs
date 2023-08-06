using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Utility
{
    public class ByteBuffer
    {
        private byte[] bytes = new byte[1];
        private int offset = 0;
        private BinaryFormatter bf = new BinaryFormatter();

        public void Append<T>(T data, bool le=true)
        {
            MemoryStream ms = new();
            bf.Serialize(ms, data);
            byte[] _bytes = ms.ToArray();
            ms.Close();

            if (le) Array.Reverse(bytes);

            // if not enough space, allocate more
            while (offset + _bytes.Length >= bytes.Length ) {
                byte[] newBytes = new byte[bytes.Length * 2];

                for ( int i = 0; i < bytes.Length; i++ )
                {
                    newBytes[i] = bytes[i];
                }
                bytes = newBytes;
            }

            for ( int i = 0; i < _bytes.Length; i++ ) {
                bytes[i + offset] = _bytes[i];
            }
            offset += _bytes.Length;
        }

    }
}
