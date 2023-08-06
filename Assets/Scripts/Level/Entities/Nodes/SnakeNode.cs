using Kaitai;
using SMM2Level;

namespace SMM2Level.Entities.Nodes
{
    public class SnakeNode : Entity
    {
        ushort index;
        ushort direction;
        uint unknown1;

        public override void LoadFromStream(KaitaiStream io)
        {
            index = io.ReadU2le();
            direction = io.ReadU2le();
            unknown1 = io.ReadU4le();
        }
    }
}