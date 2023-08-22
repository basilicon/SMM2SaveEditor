using Avalonia.Controls;
using System.Diagnostics;
using System.Collections.Generic;

namespace SMM2SaveEditor.Utility.EditorHelpers
{
    public partial class FlagEditor : UserControl
    {
        public uint flag = 0;

        ContentControl contentControl;

        Grid grid;
        List<CheckBox> checkedList = new();

        TextBox textBox;
        Button button;

        bool bIsExpanded = false;

        public FlagEditor()
        {
            InitializeComponent();

            contentControl = this.Find<ContentControl>("FlagEditorArea");

            textBox = new();
            textBox.Text = flag.ToString("X");
            textBox.TextChanged += TextBox_TextChanged;
            contentControl.Content = textBox;

            grid = new();
            SetupGrid();

            button = this.Find<Button>("ToggleExpand");
            button.Click += Button_Click;
        }

        public void SetFlag(uint newFlag)
        {
            flag = newFlag;
            textBox.Text = flag.ToString("X");
            UpdateGrid();
        }

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (bIsExpanded)
            {
                textBox.Text = flag.ToString("X");
                contentControl.Content = textBox;
                button.Content = "Expand";
            } 
            else
            {
                UpdateGrid();
                contentControl.Content = grid;
                button.Content = "Contract";
            }

            bIsExpanded ^= true;
        }

        private void TextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (textBox.Text == "")
            {
                flag = 0; return;
            }

            try
            {
                flag = uint.Parse(textBox.Text, System.Globalization.NumberStyles.AllowHexSpecifier);
            }
            catch
            {
                Debug.WriteLine($"The textbox contained an illegal flag: {textBox.Text}");
            }
        }

        private void SetupGrid()
        {
            uint counter = 1;
            int size = sizeof(uint) * 8;

            grid.RowDefinitions = new RowDefinitions(ObjectExtensions.EqualSpacingDefinition(size));

            for (int i = 0; i < size; i++)
            {
                CheckBox checkBox = new();
                checkBox.Content = "0x" + counter.ToString("X");
                checkBox.IsChecked = (counter & flag) != 0;
                grid.Children.Add(checkBox);
                checkedList.Add(checkBox);
                Grid.SetRow(checkBox, i);

                uint copy = counter;
                checkBox.Click += delegate { flag ^= copy; };

                counter <<= 1;
            }
        }

        private void UpdateGrid()
        {
            uint counter = 1;

            for (int i = 0; i < checkedList.Count; i++) 
            {
                checkedList[i].IsChecked = (counter & flag) != 0;
                counter <<= 1;
            }
        }
    }
}
