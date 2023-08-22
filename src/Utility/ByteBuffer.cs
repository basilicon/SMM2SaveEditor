using System;

namespace SMM2SaveEditor.Utility
{
    public class ByteBuffer
    {
        public byte[] bytes = new byte[0];
        public int offset = 0;

        public ByteBuffer(int capacity = 1) 
        {
            bytes = new byte[capacity];
        }

        public void Append<T>(T data, bool le=true)
        {
            if (data == null) throw new ArgumentNullException("Could not append null value to ByteBuffer!");

            object evilData = (object)data;
            byte[] _bytes;

            switch (typeof(T)) // evil hack
            {
                case var v when v == typeof(int): _bytes = BitConverter.GetBytes((int)evilData); break;
                case var v when v == typeof(uint): _bytes = BitConverter.GetBytes((uint)evilData); break;
                case var v when v == typeof(long): _bytes = BitConverter.GetBytes((long)evilData); break;
                case var v when v == typeof(ulong): _bytes = BitConverter.GetBytes((ulong)evilData); break;
                case var v when v == typeof(short): _bytes = BitConverter.GetBytes((short)evilData); break;
                case var v when v == typeof(ushort): _bytes = BitConverter.GetBytes((ushort)evilData); break;
                default:
                    throw new ArgumentException("Illegal type detected!");
            }

            if (!le) Array.Reverse(_bytes);
            Append(_bytes);
        }

        public void Append(byte data, bool le=true)
        {
            Append(new byte[] { data }, le);
        }

        public void Append<T>(T[] datas, bool le=true)
        {
            foreach( T t in datas )
            {
                Append(t, le);
            }
        }

        public void Append(byte[] data,  bool le=true)
        {
            // if not enough space, allocate more
            while (offset + data.Length >= bytes.Length)
            {
                byte[] newBytes = new byte[bytes.Length * 2];

                for (int i = 0; i < bytes.Length; i++)
                {
                    newBytes[i] = bytes[i];
                }
                bytes = newBytes;
            }

            for (int i = 0; i < data.Length; i++)
            {
                bytes[i + offset] = data[i];
            }
            offset += data.Length;
        }

        public byte[] GetBytes()
        {
            byte[] outBytes = new byte[offset];
            for (int i = 0; i < offset; i++)
            {
                outBytes[i] = bytes[i];
            }
            return outBytes;
        }
    }
}
