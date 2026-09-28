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
using System.Collections.Generic;

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

        public Point? dragStartPos;
        public bool isDragging;

        public virtual bool TryMoveBy(int deltaTilesX, int deltaTilesY)
        {
            return false;
        }

        public void OnClick(object? sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(sender as Visual);
            bool isLeft = point.Properties.IsLeftButtonPressed || point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed;
            bool isRight = point.Properties.IsRightButtonPressed || point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed;
            if (!isLeft && !isRight)
                return;

            Entity? targetEntity = (sender as Entity) ?? (sender as Visual)?.FindAncestorOfType<Entity>() ?? this;
            if (targetEntity != null)
            {
                e.Handled = true;

                var map = targetEntity.FindAncestorOfType<Map>() ?? (targetEntity as Map);
                var canvas = map?.Find<Canvas>("MapCanvas");
                List<Entity>? overlapping = null;

                if (map != null && canvas != null)
                {
                    Point clickPos = e.GetPosition(canvas);
                    overlapping = map.GetOverlappingEntities(clickPos, targetEntity);
                }

                if (overlapping != null && overlapping.Count > 1)
                {
                    // If an entity was already selected and is in the overlapping list, cycle to the next on repeated click
                    if (EntityEditor.Instance != null && EntityEditor.Instance.SelectedEntity != null && overlapping.Contains(EntityEditor.Instance.SelectedEntity))
                    {
                        int currentIndex = overlapping.IndexOf(EntityEditor.Instance.SelectedEntity);
                        int nextIndex = (currentIndex + 1) % overlapping.Count;
                        targetEntity = overlapping[nextIndex];
                    }
                    else if (!overlapping.Contains(targetEntity))
                    {
                        targetEntity = overlapping[0];
                    }
                }

                if (EntityEditor.Instance != null)
                {
                    EntityEditor.Instance.OpenOptions(targetEntity, overlapping);
                }

                if (isLeft && !(targetEntity is Level) && !(targetEntity is Map))
                {
                    if (canvas != null)
                    {
                        targetEntity.isDragging = true;
                        targetEntity.dragStartPos = e.GetPosition(canvas);
                        e.Pointer.Capture(targetEntity);
                    }
                }
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (isDragging && dragStartPos.HasValue)
            {
                var map = this.FindAncestorOfType<Map>();
                var canvas = map?.Find<Canvas>("MapCanvas");
                if (canvas != null)
                {
                    var currentPos = e.GetPosition(canvas);
                    double diffX = currentPos.X - dragStartPos.Value.X;
                    double diffY = dragStartPos.Value.Y - currentPos.Y; // inverted Y

                    int deltaTilesX = (int)(diffX / 160.0);
                    int deltaTilesY = (int)(diffY / 160.0);

                    if (deltaTilesX != 0 || deltaTilesY != 0)
                    {
                        if (TryMoveBy(deltaTilesX, deltaTilesY))
                        {
                            dragStartPos = new Point(
                                dragStartPos.Value.X + deltaTilesX * 160.0,
                                dragStartPos.Value.Y - deltaTilesY * 160.0);

                            map?.HighlightEntity(this);
                        }
                    }
                }
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (isDragging)
            {
                isDragging = false;
                dragStartPos = null;
                e.Pointer.Capture(null);

                // Refresh inspector to reflect final moved coordinates
                if (EntityEditor.Instance != null && EntityEditor.Instance.SelectedEntity == this)
                {
                    var map = this.FindAncestorOfType<Map>();
                    var canvas = map?.Find<Canvas>("MapCanvas");
                    List<Entity>? overlapping = null;
                    if (map != null && canvas != null)
                    {
                        Point currentPos = e.GetPosition(canvas);
                        overlapping = map.GetOverlappingEntities(currentPos, this);
                    }
                    EntityEditor.Instance.OpenOptions(this, overlapping);
                }
            }
        }

        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            base.OnPointerCaptureLost(e);
            isDragging = false;
            dragStartPos = null;
        }
    }
}
