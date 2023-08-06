using Kaitai;

namespace SMM2Level.Entities.Nodes
{
    public class TrackBlockNode : Entity
    {
        byte unknown1;
        byte direction;
        ushort unknown2;

        public override void LoadFromStream(KaitaiStream io)
        {
            unknown1 = io.ReadU1();
            direction = io.ReadU1();
            unknown2 = io.ReadU2le();
        }
    }
}
