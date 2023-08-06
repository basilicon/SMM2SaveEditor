using Kaitai;

namespace SMM2Level.Entities.Nodes
{
    public class ClearPipeNode : Entity
    {
        byte type;
        byte index;
        byte x;
        byte y;
        byte width;
        byte height;
        byte unknown1;
        byte direction;

        public override void LoadFromStream(KaitaiStream io)
        {
            type = io.ReadU1();
            index = io.ReadU1();
            x = io.ReadU1();
            y = io.ReadU1();
            width = io.ReadU1();
            height = io.ReadU1();
            unknown1 = io.ReadByte();
            direction = io.ReadByte();
        }
    }
}
