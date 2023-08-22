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

        Grid grid;

        public EntityEditor()
        {
            Instance = this;
            InitializeComponent();

            grid = this.Find<Grid>("EditorGrid");

        }

        // TODO: MOVE MENU TO CANVAS IN MAINWINDOW
        // TODO: ADD SUPPORT FOR FORMATTING
        public void OpenOptions(Point p, IEntity entity)
        {
            grid.Children.RemoveAll(grid.Children);

            IDictionary<string, object> options = entity.AsDictionary(entity.GetType());

            grid.ColumnDefinitions = new ColumnDefinitions("2*,*");
            grid.RowDefinitions = new RowDefinitions(ObjectExtensions.EqualSpacingDefinition(options.Count));

            int counter = 0;
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
            }
        }
    }
}
