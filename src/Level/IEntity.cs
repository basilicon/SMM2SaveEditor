using Kaitai;
using Avalonia.Controls;
using System.Collections.Generic;

namespace SMM2Level
{
    public interface IEntity
    {
        public byte[] GetBytes();
        public void LoadFromStream(KaitaiStream io, Canvas? canvas = null);
    }
}
