using Kaitai;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia;
using System.Diagnostics;
using SMM2SaveEditor.Utility.EditorHelpers;
using Avalonia.VisualTree;
using Avalonia.Media;
using System;
using Avalonia.LogicalTree;

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

            if (EntityEditor.Instance != null && sender is Visual visual)
            {
                // add border or smth
                //Border border = new()
                //{
                //    BorderBrush = Brushes.Black,
                //    BorderThickness = new Thickness(2),
                //    CornerRadius = new CornerRadius(2)
                //};
                //UserControl userControl = visual.FindAncestorOfType<UserControl>();
                //userControl.Content = border;

                IEntity? styledElement = visual.FindAncestorOfType<IEntity>();

                EntityEditor.Instance.OpenOptions(e.GetCurrentPoint(visual).Position, styledElement);
            }

            
        }
    }
}
