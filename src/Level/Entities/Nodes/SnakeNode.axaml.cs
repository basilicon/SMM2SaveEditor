using Avalonia.Controls;
using Kaitai;
using SMM2Level;
using SMM2Level.Utility;

namespace SMM2Level.Entities.Nodes
{
    public partial class SnakeNode : Entity
    {
        ushort index;
        ushort direction;
        uint unknown1;

        public override void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
        {
            index = io.ReadU2le();
            direction = io.ReadU2le();
            unknown1 = io.ReadU4le();
        }

        public override byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(8);

            bb.Append(index);
            bb.Append(direction);
            bb.Append(unknown1);

            return bb.GetBytes();
        }
    }
}