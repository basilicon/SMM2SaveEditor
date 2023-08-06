using Kaitai;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SMM2Level.Entities
{
    public class Ground : Entity
    {
        byte x;
        byte y;
        byte id;
        byte backgroundId;

        public override void LoadFromStream(KaitaiStream io)
        {
            x = io.ReadU1();
            y = io.ReadU1();
            id = io.ReadU1();
            backgroundId = io.ReadU1();

            transform.position = new Vector2(x, y);
        }
    }
}