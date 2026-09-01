using Avalonia.Controls;
using System.Diagnostics;
using System.Collections.Generic;
using System;

namespace SMM2SaveEditor.Utility.EditorHelpers
{
    public partial class FlagEditor : UserControl
    {
        public uint flag = 0;

        ContentControl flagSummaryArea;
        ContentControl flagEditorArea;

        ScrollViewer scrollViewer;
        Grid grid;
        List<CheckBox> checkedList = new();

        TextBox textBox;
        Button toggleExpandButton;

        bool bIsExpanded = false;

        public delegate void FlagEditorValueChangedHandler(object sender);
        public event FlagEditorValueChangedHandler ValueChanged;

        public int? objId = null;

        public FlagEditor()
        {
            InitializeComponent();

            flagSummaryArea = this.Find<ContentControl>("FlagSummaryArea")!;
            flagEditorArea = this.Find<ContentControl>("FlagEditorArea")!;

            textBox = new();
            textBox.Text = flag.ToString("X");
            textBox.TextChanged += TextBox_TextChanged;
            flagSummaryArea.Content = textBox;

            scrollViewer = new();
            scrollViewer.MaxHeight = 350;
            scrollViewer.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
            scrollViewer.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;

            grid = new();
            SetupGrid();

            toggleExpandButton = this.Find<Button>("ToggleExpand")!;
            toggleExpandButton.Click += Button_Click;
        }

        public void SetFlag(uint newFlag, int? newObjId = null)
        {
            flag = newFlag;
            objId = newObjId;
            textBox.Text = flag.ToString("X");
            UpdateGrid();
        }

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (bIsExpanded)
            {
                flagEditorArea.Content = null;
                toggleExpandButton.Content = "Expand";
            } 
            else
            {
                UpdateGrid();
                flagEditorArea.Content = scrollViewer;
                toggleExpandButton.Content = "Minimize";
            }

            bIsExpanded ^= true;
        }

        private void TextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBox.Text))
            {
                flag = 0; return;
            }

            try
            {
                flag = uint.Parse(textBox.Text, System.Globalization.NumberStyles.AllowHexSpecifier);
                UpdateCheckboxes();
                ValueChanged?.Invoke(this);
            }
            catch
            {
                Debug.WriteLine($"The textbox contained an illegal flag: {textBox.Text}");
            }
        }

        private void SetupGrid()
        {
            grid.Children.Clear();
            checkedList.Clear();

            int size = sizeof(uint) * 8;
            grid.RowDefinitions = new RowDefinitions(string.Join(",", System.Linq.Enumerable.Repeat("Auto", size + 1)));

            uint counter = 1;
            int rowIndex = 0;

            string? notes = FlagDatabase.GetObjectNotes(objId);
            if (!string.IsNullOrEmpty(notes))
            {
                TextBlock notesBlock = new TextBlock
                {
                    Text = notes,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    FontStyle = Avalonia.Media.FontStyle.Italic,
                    Foreground = Avalonia.Media.Brushes.Gray,
                    Margin = new Avalonia.Thickness(2, 2, 2, 6)
                };
                grid.Children.Add(notesBlock);
                Grid.SetRow(notesBlock, rowIndex++);
            }

            for (int i = 0; i < size; i++)
            {
                CheckBox checkBox = new();
                string label = FlagDatabase.GetFlagLabel(objId, counter);
                string? tip = FlagDatabase.GetFlagTooltip(objId, counter);

                TextBlock textBlock = new TextBlock
                {
                    Text = label,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                };

                checkBox.Content = textBlock;
                checkBox.IsChecked = (counter & flag) != 0;
                checkBox.Margin = new Avalonia.Thickness(2);

                if (!string.IsNullOrEmpty(tip))
                {
                    ToolTip.SetTip(checkBox, tip);
                }

                grid.Children.Add(checkBox);
                checkedList.Add(checkBox);
                Grid.SetRow(checkBox, rowIndex++);

                uint copy = counter;
                checkBox.Click += delegate
                {
                    flag ^= copy;
                    textBox.Text = flag.ToString("X");
                    ValueChanged?.Invoke(this);
                };

                counter <<= 1;
            }

            scrollViewer.Content = grid;
        }

        private void UpdateCheckboxes()
        {
            uint counter = 1;
            for (int i = 0; i < checkedList.Count; i++)
            {
                checkedList[i].IsChecked = (counter & flag) != 0;
                counter <<= 1;
            }
        }

        private void UpdateGrid()
        {
            SetupGrid();
        }
    }
}
