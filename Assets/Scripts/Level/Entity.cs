using Kaitai;
using UnityEngine;

namespace SMM2Level
{
    public abstract class Entity : MonoBehaviour
    {
        public abstract void LoadFromStream(KaitaiStream io);
    }
}
