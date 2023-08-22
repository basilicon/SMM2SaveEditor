using Avalonia.Controls;
using Kaitai;
using SMM2SaveEditor;
using SMM2SaveEditor.Utility;

namespace SMM2SaveEditor.Entities.Nodes
{
    public partial class SnakeNode : UserControl, IEntity
    {
        ushort index;
        ushort direction;
        uint unknown1;

        public SnakeNode()
        {
            InitializeComponent();
        }

        public void LoadFromStream(KaitaiStream io, Canvas? canvas = null)
        {
            index = io.ReadU2le();
            direction = io.ReadU2le();
            unknown1 = io.ReadU4le();
        }

        public byte[] GetBytes()
        {
            ByteBuffer bb = new ByteBuffer(8);

            bb.Append(index);
            bb.Append(direction);
            bb.Append(unknown1);

            return bb.GetBytes();
        }

        public void UpdateSprite()
        {
            throw new System.NotImplementedException();
        }
    }
}