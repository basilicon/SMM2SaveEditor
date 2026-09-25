using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
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

        private StackPanel editorStack;
        private Border emptyStatePanel;
        private TextBlock entityTypeBadge;
        private Border entityTypeBadgeContainer;

        public EntityEditor()
        {
            Instance = this;
            InitializeComponent();

            editorStack = this.Find<StackPanel>("EditorStack")!;
            emptyStatePanel = this.Find<Border>("EmptyStatePanel")!;
            entityTypeBadge = this.Find<TextBlock>("EntityTypeBadge")!;
            entityTypeBadgeContainer = this.Find<Border>("EntityTypeBadgeContainer")!;

            UpdateVisibility();
        }

        public void OpenOptions(Entity entity)
        {
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

            UpdateVisibility();
        }

        public void ClearSelection()
        {
            objRef = null;
            editorStack.Children.Clear();
            if (entityTypeBadge != null)
            {
                entityTypeBadge.Text = string.Empty;
            }
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            bool hasEntity = objRef != null && editorStack.Children.Count > 0;
            if (emptyStatePanel != null) emptyStatePanel.IsVisible = !hasEntity;
            if (editorStack != null) editorStack.IsVisible = hasEntity;
            if (entityTypeBadgeContainer != null) entityTypeBadgeContainer.IsVisible = hasEntity;
        }
    }
}
