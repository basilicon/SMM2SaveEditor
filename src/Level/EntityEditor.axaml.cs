using Avalonia;
using Avalonia.Controls;
using System.Diagnostics;
using SMM2SaveEditor.Utility.EditorHelpers;
using SMM2SaveEditor.Utility;
using System.Collections.Generic;
using Avalonia.Media;
using System;

namespace SMM2SaveEditor
{
    public partial class EntityEditor : UserControl
    {
        public static EntityEditor? Instance { get; set; }

        IEntity? objRef = null;

        List<string> labels;
        Grid grid;

        public EntityEditor()
        {
            Instance = this;
            InitializeComponent();

            grid = this.Find<Grid>("EditorGrid");

            this.Find<Button>("Apply").Click += delegate
            {
                if (labels == null || objRef == null) return;

                IDictionary<string, object> options = new Dictionary<string, object>();

                foreach (var child in grid.Children)
                {
                    if (Grid.GetColumn(child) == 0) continue;

                    int row = Grid.GetRow(child);
                    string key = labels[row];

                    if (child is EnumDropdown dropdown)
                    {
                        options.Add(key, dropdown.enumValue);
                    }
                    else if (child is FlagEditor flagEditor)
                    {
                        options.Add(key, flagEditor.flag);
                    }
                    if (child is TextBox textBox) 
                    {
                        options.Add(key, textBox.Text);
                    }
                }

                objRef.SetValuesFromDictionary(objRef.GetType(), options);
                objRef.UpdateSprite();
            };

            this.Find<Button>("Cancel").Click += delegate
            {
                grid.Children.RemoveAll(grid.Children);
                objRef = null;
            };
        }

        // TODO: MOVE MENU TO CANVAS IN MAINWINDOW
        // TODO: ADD SUPPORT FOR FORMATTING
        public void OpenOptions(Point p, IEntity entity)
        {
            grid.Children.RemoveAll(grid.Children);

            objRef = entity;

            IDictionary<string, object> options = entity.AsDictionary(entity.GetType());

            grid.ColumnDefinitions = new ColumnDefinitions("2*,*");
            grid.RowDefinitions = new RowDefinitions(ObjectExtensions.EqualSpacingDefinition(options.Count));

            int counter = 0;
            labels = new(options.Count);
            foreach (KeyValuePair<string, object> kvp in options)
            {
                TextBlock textBlock = new();
                textBlock.Text = kvp.Key;
                textBlock.TextAlignment = TextAlignment.Center;
                textBlock.MinHeight = 20;
                grid.Children.Add(textBlock);
                Grid.SetColumn(textBlock, 0);
                Grid.SetRow(textBlock, counter);

                Type type = kvp.Value.GetType();

                Control o = new();

                if (type.IsSubclassOf(typeof(Enum))) 
                {
                    EnumDropdown enumDropdown = new();
                    enumDropdown.SetEnum(type, kvp.Value);

                    o = enumDropdown;
                } 
                else if (kvp.Key == "flag" || kvp.Key == "cflag")
                {
                    FlagEditor flagEditor = new();
                    flagEditor.SetFlag((uint)kvp.Value);
                    o = flagEditor;
                }
                else
                {
                    TextBox valueBlock = new();
                    valueBlock.Text = kvp.Value.ToString();
                    o = valueBlock;
                }

                grid.Children.Add(o);
                Grid.SetColumn(o, 1);
                Grid.SetRow(o, counter);

                counter++;
                labels.Add(kvp.Key);
            }
        }
    }
}
