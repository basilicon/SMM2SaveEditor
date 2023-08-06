using Kaitai;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SMM2Level.Entities
{
    public class Track : Entity
    {
        ushort unknown1;
        byte flags;
        byte x;
        byte y;
        byte type;
        ushort lid;
        ushort unknown2;
        ushort unknown3;

        public override void LoadFromStream(KaitaiStream io)
        {
            unknown1 = io.ReadU2le();
            flags = io.ReadU1();
            x = io.ReadU1();
            y = io.ReadU1();
            type = io.ReadU1();
            lid = io.ReadU2le();
            unknown2 = io.ReadU2le();
            unknown3 = io.ReadU2le();
        }
    }
}
