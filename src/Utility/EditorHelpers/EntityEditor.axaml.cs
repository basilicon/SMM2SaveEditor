using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using SMM2SaveEditor.Entities;
using SMM2SaveEditor.Utility;
using SMM2SaveEditor.Utility.EditorHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace SMM2SaveEditor.Utility.EditorHelpers
{
    public class OverlappingEntityItem
    {
        public Entity Entity { get; }
        public string DisplayText { get; }

        public OverlappingEntityItem(Entity entity, string displayText)
        {
            Entity = entity;
            DisplayText = displayText;
        }

        public override string ToString() => DisplayText;
    }

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

        private Border overlappingBar;
        private TextBlock overlappingCountText;
        private ComboBox overlappingDropdown;
        private bool isUpdatingOverlapping = false;
        private List<Entity>? currentOverlappingList = null;

        public EntityEditor()
        {
            Instance = this;
            InitializeComponent();

            editorStack = this.Find<StackPanel>("EditorStack")!;
            emptyStatePanel = this.Find<Border>("EmptyStatePanel")!;
            entityTypeBadge = this.Find<TextBlock>("EntityTypeBadge")!;
            entityTypeBadgeContainer = this.Find<Border>("EntityTypeBadgeContainer")!;
            deleteEntityBtn = this.Find<Button>("DeleteEntityBtn");

            overlappingBar = this.Find<Border>("OverlappingBar")!;
            overlappingCountText = this.Find<TextBlock>("OverlappingCountText")!;
            overlappingDropdown = this.Find<ComboBox>("OverlappingDropdown")!;
            overlappingDropdown.SelectionChanged += OnOverlappingSelectionChanged;

            UpdateVisibility();
        }

        public static string GetEntityDisplayName(Entity entity)
        {
            if (entity is Obj obj)
                return $"Obj: {obj.id} ({obj.x / 160}, {obj.y / 160})";
            if (entity is SoundEffect sfx)
                return $"Sound: {sfx.id} ({sfx.x}, {sfx.y})";
            if (entity is Track trk)
                return $"Track: {trk.type} ({trk.x}, {trk.y})";
            if (entity is Ground grnd)
                return $"Ground ({grnd.x}, {grnd.y})";
            if (entity is Icicle icicle)
                return $"Icicle: {icicle.type} ({icicle.x}, {icicle.y})";
            if (entity is Snake snake)
                return $"Snake Block ({snake.nodes?.Count ?? 0} nodes)";
            if (entity is ClearPipe pipe)
                return $"Clear Pipe ({pipe.nodes?.Count ?? 0} nodes)";
            if (entity is PiranhaCreeper creeper)
                return $"Piranha Creeper ({creeper.nodes?.Count ?? 0} nodes)";
            if (entity is ExclamationBlock eb)
                return $"! Block ({eb.nodes?.Count ?? 0} nodes)";
            if (entity is TrackBlock tb)
                return $"Track Block ({tb.nodes?.Count ?? 0} nodes)";
            return entity.GetType().Name;
        }

        public bool AreOverlappingEntitiesCurrent(List<Entity>? overlapping)
        {
            if (currentOverlappingList == null || overlapping == null) return false;
            if (currentOverlappingList.Count != overlapping.Count) return false;
            for (int i = 0; i < currentOverlappingList.Count; i++)
            {
                if (currentOverlappingList[i] != overlapping[i]) return false;
            }
            return true;
        }

        public void SelectEntity(Entity ent)
        {
            if (objRef == ent) return;

            if (objRef != null)
            {
                objRef.FindAncestorOfType<Map>()?.HighlightEntity(null);
            }

            objRef = ent;
            if (entityTypeBadge != null)
            {
                entityTypeBadge.Text = ent.GetType().Name;
            }

            // Update active styling on cards
            foreach (var child in editorStack.Children)
            {
                if (child is ObjectEditor oe)
                {
                    oe.SetActive(oe.TargetEntity == ent);
                }
            }

            // Sync dropdown selection without reloading
            if (overlappingDropdown != null && currentOverlappingList != null)
            {
                isUpdatingOverlapping = true;
                if (overlappingDropdown.ItemsSource is List<OverlappingEntityItem> items)
                {
                    int index = items.FindIndex(i => i.Entity == ent);
                    if (index >= 0) overlappingDropdown.SelectedIndex = index;
                }
                isUpdatingOverlapping = false;
            }

            RefreshSelectionHighlight();
            UpdateVisibility();
        }

        public void OpenOptions(Entity entity, List<Entity>? overlapping = null)
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

            currentOverlappingList = overlapping;
            var entitiesToDisplay = (overlapping != null && overlapping.Count > 1)
                ? overlapping
                : new List<Entity> { entity };

            foreach (var ent in entitiesToDisplay)
            {
                foreach (Type t in ent.GetType().GetInheritanceHierarchy())
                {
                    ObjectEditor objectEditor = new();
                    string header = entitiesToDisplay.Count > 1 ? GetEntityDisplayName(ent) : t.Name;
                    objectEditor.OpenOptions((Convert.ChangeType(ent, t) as Entity)!, header);
                    objectEditor.SetActive(ent == entity);

                    var currentEnt = ent;
                    objectEditor.Activated += (s, e) =>
                    {
                        SelectEntity(currentEnt);
                    };

                    editorStack.Children.Add(objectEditor);
                }
            }

            // Update overlapping dropdown
            if (overlapping != null && overlapping.Count > 1)
            {
                isUpdatingOverlapping = true;
                overlappingBar.IsVisible = true;
                overlappingCountText.Text = $"Overlapping ({overlapping.Count}):";

                var items = new List<OverlappingEntityItem>();
                for (int i = 0; i < overlapping.Count; i++)
                {
                    string name = GetEntityDisplayName(overlapping[i]);
                    items.Add(new OverlappingEntityItem(overlapping[i], name));
                }

                overlappingDropdown.ItemsSource = items;
                int selectedIndex = items.FindIndex(i => i.Entity == entity);
                overlappingDropdown.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
                isUpdatingOverlapping = false;
            }
            else
            {
                overlappingBar.IsVisible = false;
                overlappingDropdown.ItemsSource = null;
            }

            // Highlight the newly selected entity on its map
            RefreshSelectionHighlight();
            UpdateVisibility();
        }

        private void OnOverlappingSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingOverlapping) return;
            if (overlappingDropdown.SelectedItem is OverlappingEntityItem item && item.Entity != objRef)
            {
                SelectEntity(item.Entity);
                foreach (var child in editorStack.Children)
                {
                    if (child is ObjectEditor oe && oe.TargetEntity == item.Entity)
                    {
                        oe.Expand();
                        oe.BringIntoView();
                    }
                }
            }
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
            currentOverlappingList = null;
            if (overlappingBar != null) overlappingBar.IsVisible = false;
            if (overlappingDropdown != null) overlappingDropdown.ItemsSource = null;

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
                var remainingOverlapping = currentOverlappingList != null
                    ? new List<Entity>(currentOverlappingList)
                    : null;
                remainingOverlapping?.Remove(target);

                ClearSelection();
                map.RemoveEntity(target);

                if (remainingOverlapping != null && remainingOverlapping.Count > 0)
                {
                    OpenOptions(remainingOverlapping[0], remainingOverlapping.Count > 1 ? remainingOverlapping : null);
                }
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
            if (!hasEntity && overlappingBar != null) overlappingBar.IsVisible = false;
        }
    }
}
