using Avalonia.Controls;
using System;
using System.Collections.Generic;
using SMM2SaveEditor.Entities;
using System.Diagnostics;

namespace SMM2SaveEditor.Utility.EditorHelpers
{
    public partial class EnumDropdown : UserControl
    {
        private ComboBox dropdown;

        public object? enumValue;

        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        public EnumDropdown()
        {
            InitializeComponent();

            dropdown = this.Find<ComboBox>("Dropdown");
        }

        public void SetEnum<T>(T defaultValue) where T : Enum
        {
            SetEnum(typeof(T), defaultValue);
        }

        public void SetEnum(Type T, object? defaultValue = null)
        {
            List<string> enums = new(Enum.GetNames(T));
            enums.Sort();

            for (int i = 0; i < enums.Count; i++)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = enums[i].ToString();
                dropdown.Items.Add(item);
            }

            dropdown.SelectionChanged += (sender, e) =>
            {
                if (dropdown.SelectedValue is ComboBoxItem item) {
                    enumValue = Enum.Parse(T, item.Content.ToString());
                    SelectionChanged.Invoke(this, e);
                }
            };

            if (defaultValue != null)
            {
                SetValue(defaultValue.ToString());
                enumValue = defaultValue;
            }
        }

        private void SetValue(string info)
        {
            dropdown.PlaceholderText = info;
            dropdown.SelectedValue = info;
        }
    }
}
