using Kaitai;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia;
using System.Diagnostics;
using SMM2SaveEditor.Utility.EditorHelpers;
using Avalonia.VisualTree;
using Avalonia.Media;
using System;
using System.Collections;

namespace SMM2SaveEditor
{
    public abstract class Entity : UserControl
    {
        public Entity? ParentEntity;

        public abstract byte[] GetBytes();
        public abstract void LoadFromStream(KaitaiStream io);
        public virtual void UpdateSprite()
        {
            ParentEntity?.UpdateSprite();
        }

        public void OnClick(object? sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(sender as Visual);
            if (!point.Properties.IsRightButtonPressed && point.Properties.PointerUpdateKind != PointerUpdateKind.RightButtonPressed)
                return;

            if (EntityEditor.Instance != null)
            {
                Entity? targetEntity = (sender as Entity) ?? (sender as Visual)?.FindAncestorOfType<Entity>() ?? this;
                if (targetEntity != null)
                {
                    e.Handled = true;
                    EntityEditor.Instance.OpenOptions(targetEntity);
                }
            }
        }
    }
}
