using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using SMM2SaveEditor.Utility;
using SMM2SaveEditor.Utility.EditorHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SMM2SaveEditor.Utility.EditorHelpers
{
    public partial class ObjectEditor : UserControl
    {
        public static ObjectEditor? Instance { get; set; }

        private Entity? objRef = null;
        private List<string> labels = new(0);

        private StackPanel rootStackPanel;
        private Grid grid;
        private TextBlock editorHeader;
        private ToggleButton expandButton;

        public ObjectEditor()
        {
            Instance = this;
            InitializeComponent();

            rootStackPanel = this.Find<StackPanel>("RootStackPanel")!;

            grid = this.Find<Grid>("EditorGrid")!;

            editorHeader = this.Find<TextBlock>("EditorHeader")!;

            expandButton = this.Find<ToggleButton>("ExpandButton")!;
            expandButton.Click += (s, e) =>
            {
                if (expandButton.IsChecked!.Value == false) // i hate null possible
                {
                    HideEditorGrid();
                } else
                {
                    ShowEditorGrid();
                }
            };
        }

        private void ShowEditorGrid()
        {
            if (rootStackPanel.Children.Contains(grid)) return;

            rootStackPanel.Children.Add(grid);
            expandButton.Content = "v";
        }

        private void HideEditorGrid()
        {
            rootStackPanel.Children.Remove(grid);
            expandButton.Content = ">";
        }

        // DEPRECATED
        private void OnApply()
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
                    options.Add(key, dropdown.enumValue!);
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
                    options.Add(key, textBox.Text!);
                }
            }

            objRef.SetValuesFromDictionary(objRef.GetType(), options);
            objRef.UpdateSprite();
        }

        private void ApplyOption(string key, object value)
        {
            if (objRef == null)
            {
                Debug.WriteLine("Object reference not set!");
                return;
            }

            Type type = objRef.GetType();
            var field = type.GetField(key);

            if (field == null)
            {
                Debug.WriteLine($"Property {key} is invalid!");
                return;
            }

            field.SetValue(objRef, Convert.ChangeType(value, field.FieldType));
        }

        public void OpenOptions(Entity entity)
        {
            grid.Children.RemoveAll(grid.Children);

            objRef = entity;

            Type entityType = entity.GetType();
            editorHeader.Text = entityType.Name;
            IDictionary<string, object> options = entity.AsDictionary(entityType);

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
                    enumDropdown.SelectionChanged += (o, e) => {
                        ApplyOption(kvp.Key, (o as EnumDropdown)!.enumValue!);
                    };

                    o = enumDropdown;
                }
                else
                if (kvp.Key == "flag" || kvp.Key == "cflag")
                {
                    FlagEditor flagEditor = new();
                    flagEditor.SetFlag((uint)kvp.Value);
                    flagEditor.ValueChanged += (o) =>
                    {
                        ApplyOption(kvp.Key, (o as FlagEditor)!.flag);
                    };

                    o = flagEditor;
                }
                else
                if (IsIntegerType(type))
                {
                    NumericUpDown numericUpDown = new();
                    numericUpDown.Increment = 1;
                    numericUpDown.Value = Convert.ToDecimal(kvp.Value);
                    numericUpDown.ValueChanged += (o, e) => {
                        ApplyOption(kvp.Key, Convert.ToInt64((o as NumericUpDown)!.Value));
                    };

                    o = numericUpDown;
                }
                else
                if (type == typeof(string))
                {
                    TextBox textBox = new();
                    textBox.Text = (string)kvp.Value;
                    textBox.TextChanged += (o, e) => {
                        ApplyOption(kvp.Key, (o as TextBox)!.Text!);
                    };

                    textBox.TextWrapping = TextWrapping.Wrap;
                    textBox.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
                    textBox.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;

                    o = textBox;
                }
                else
                {
                    TextBox valueBlock = new();
                    valueBlock.Text = kvp.Value.ToString();
                    valueBlock.TextInput += (o, e) => {
                        ApplyOption(kvp.Key, (o as TextBox)!.Text!);
                    };

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
