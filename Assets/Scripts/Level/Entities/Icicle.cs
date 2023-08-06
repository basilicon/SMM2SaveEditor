using Kaitai;
using UnityEngine;

namespace SMM2Level.Entities
{
    public class Icicle : Entity
    {
        byte x;
        byte y;
        byte type;
        byte unknown1;

        public override void LoadFromStream(KaitaiStream io)
        {
            x = io.ReadU1();
            y = io.ReadU1();
            type = io.ReadU1();
            unknown1 = io.ReadU1();

            transform.position = new Vector2(x, y);
        }
    }
}