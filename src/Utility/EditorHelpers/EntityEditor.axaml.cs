using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SMM2SaveEditor.Utility;
using SMM2SaveEditor.Utility.EditorHelpers;
using System;
using System.Collections.Generic;

namespace SMM2SaveEditor.Utility.EditorHelpers
{
    public partial class EntityEditor : UserControl
    {
        public static EntityEditor? Instance { get; set; }

        private IEntity? objRef = null;

        private List<string> labels;
        private Grid grid;

        private Button applyButton;
        private Button cancelButton;

        public EntityEditor()
        {
            Instance = this;
            InitializeComponent();

            grid = this.Find<Grid>("EditorGrid");
            if (grid == null) throw new Exception("No grid found!");

            applyButton = this.Find<Button>("Apply");
            if (applyButton == null) throw new Exception("No apply button found!");

            applyButton.Click += OnApply;

            cancelButton = this.Find<Button>("Cancel");
            if (cancelButton == null) throw new Exception("No cancel button found!");

            cancelButton.Click += delegate
            {
                grid.Children.RemoveAll(grid.Children);
                objRef = null;
            };
        }

        private void OnApply(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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
                else if (child is NumericUpDown numericUpDown)
                {
                    options.Add(key, Convert.ToInt32(numericUpDown.Value));
                }
                if (child is TextBox textBox)
                {
                    options.Add(key, textBox.Text);
                }
            }

            objRef.SetValuesFromDictionary(objRef.GetType(), options);
            objRef.UpdateSprite();
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
                textBlock.Margin = new Thickness(5);

                Type type = kvp.Value.GetType();

                Control o = new();

                if (type.IsSubclassOf(typeof(Enum))) 
                {
                    EnumDropdown enumDropdown = new();
                    enumDropdown.SetEnum(type, kvp.Value);

                    o = enumDropdown;
                } 
                else 
                if (kvp.Key == "flag" || kvp.Key == "cflag")
                {
                    FlagEditor flagEditor = new();
                    flagEditor.SetFlag((uint)kvp.Value);
                    o = flagEditor;
                }
                else 
                if (IsIntegerType(type))
                {
                    NumericUpDown numericUpDown = new();
                    numericUpDown.Increment = 1;
                    numericUpDown.Value = Convert.ToDecimal(kvp.Value);

                    o = numericUpDown;
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
                o.Margin = new Thickness(5);

                counter++;
                labels.Add(kvp.Key);
            }
        }

        private static bool IsIntegerType(Type t)
        {
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return true;
                default:
                    return false;
            }
        }
    }
}
