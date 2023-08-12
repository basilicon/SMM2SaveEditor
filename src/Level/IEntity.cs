using Kaitai;
using Avalonia.Controls;

namespace SMM2Level
{
    public interface IEntity
    {
        public byte[] GetBytes();
        public void LoadFromStream(KaitaiStream io, Canvas? canvas = null);
    }
}
