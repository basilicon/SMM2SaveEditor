using Kaitai;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia;
using System.Diagnostics;
using SMM2SaveEditor.Entities;
using Avalonia.VisualTree;

namespace SMM2SaveEditor
{
    public interface IEntity
    {
        public byte[] GetBytes();
        public void LoadFromStream(KaitaiStream io, Canvas? canvas = null);
        public void UpdateSprite();

        public void OnClick(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as Visual).Properties.PointerUpdateKind != PointerUpdateKind.RightButtonPressed) return;

            if (EntityEditor.Instance != null && sender is Image image)
            {
                var styledElement = image.FindAncestorOfType<IEntity>(); 

                EntityEditor.Instance.OpenOptions(e.GetCurrentPoint(image).Position, styledElement);
            }
        }
    }
}
