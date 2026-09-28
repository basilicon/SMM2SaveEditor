using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using SMM2SaveEditor.Utility;
using SMM2SaveEditor.Utility.EditorHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace SMM2SaveEditor.Utility.EditorHelpers
{
    public partial class EntityEditor : UserControl
    {
        public static EntityEditor? Instance { get; set; }

        private Entity? objRef = null;
        public Entity? SelectedEntity => objRef;

        private StackPanel editorStack;
        private Border emptyStatePanel;
        private TextBlock entityTypeBadge;
        private Border entityTypeBadgeContainer;
        private Button? deleteEntityBtn;

        public EntityEditor()
        {
            Instance = this;
            InitializeComponent();

            editorStack = this.Find<StackPanel>("EditorStack")!;
            emptyStatePanel = this.Find<Border>("EmptyStatePanel")!;
            entityTypeBadge = this.Find<TextBlock>("EntityTypeBadge")!;
            entityTypeBadgeContainer = this.Find<Border>("EntityTypeBadgeContainer")!;
            deleteEntityBtn = this.Find<Button>("DeleteEntityBtn");

            UpdateVisibility();
        }

        public void OpenOptions(Entity entity)
        {
            // Clear any highlight on previous entity
            if (objRef != null && objRef != entity)
            {
                objRef.FindAncestorOfType<Map>()?.HighlightEntity(null);
            }

            editorStack.Children.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            objRef = entity;
            if (entityTypeBadge != null)
            {
                entityTypeBadge.Text = entity.GetType().Name;
            }

            foreach (Type t in entity.GetType().GetInheritanceHierarchy())
            {
                ObjectEditor objectEditor = new();
                editorStack.Children.Add(objectEditor);
                objectEditor.OpenOptions((Convert.ChangeType(entity, t) as Entity)!);

                Debug.WriteLine(t.Name);
            }

            // Highlight the newly selected entity on its map
            RefreshSelectionHighlight();
            UpdateVisibility();
        }

        public void RefreshSelectionHighlight()
        {
            if (objRef != null)
            {
                objRef.FindAncestorOfType<Map>()?.HighlightEntity(objRef);
            }
        }

        public void ClearSelection()
        {
            if (objRef != null)
            {
                objRef.FindAncestorOfType<Map>()?.HighlightEntity(null);
            }

            objRef = null;
            editorStack.Children.Clear();
            if (entityTypeBadge != null)
            {
                entityTypeBadge.Text = string.Empty;
            }
            UpdateVisibility();
        }

        public void DeleteSelectedEntity()
        {
            if (objRef == null || objRef is Level || objRef is Map) return;

            var topLevel = TopLevel.GetTopLevel(this);
            var focused = topLevel?.FocusManager?.GetFocusedElement();
            if (focused is TextBox) return;

            var map = objRef.FindAncestorOfType<Map>();
            if (map != null)
            {
                var target = objRef;
                ClearSelection();
                map.RemoveEntity(target);
            }
        }

        private void OnDeleteEntityClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            DeleteSelectedEntity();
        }

        private void UpdateVisibility()
        {
            bool hasEntity = objRef != null && editorStack.Children.Count > 0;
            if (emptyStatePanel != null) emptyStatePanel.IsVisible = !hasEntity;
            if (editorStack != null) editorStack.IsVisible = hasEntity;
            if (entityTypeBadgeContainer != null) entityTypeBadgeContainer.IsVisible = hasEntity;
            if (deleteEntityBtn != null) deleteEntityBtn.IsVisible = hasEntity && !(objRef is Level || objRef is Map);
        }
    }
}
