using Kaitai;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SMM2Level.Entities
{
    public class Sound : Entity
    {
        byte id;
        byte x;
        byte y;
        byte unknown1;

        public override void LoadFromStream(KaitaiStream io)
        {
            id = io.ReadU1();
            x = io.ReadU1();
            y = io.ReadU1();
            unknown1 = io.ReadU1();

            transform.position = new Vector2(x, y);
        }
    }
}
