using Avalonia.Controls;
using System;
using System.Globalization;

namespace SMM2SaveEditor.Utility.EditorHelpers
{
    public partial class TrackEndpointEditor : UserControl
    {
        public event Action<ushort>? ValueChanged;

        private ComboBox socketDropdown;
        private CheckBox cappedCheckBox;
        private TextBox hexTextBox;

        public ushort Value { get; private set; }
        private bool isUpdating = false;

        public TrackEndpointEditor()
        {
            InitializeComponent();

            socketDropdown = this.FindControl<ComboBox>("SocketDropdown")!;
            cappedCheckBox = this.FindControl<CheckBox>("CappedCheckBox")!;
            hexTextBox = this.FindControl<TextBox>("HexTextBox")!;

            socketDropdown.ItemsSource = Enum.GetValues(typeof(TrackSocket));

            socketDropdown.SelectionChanged += (s, e) =>
            {
                if (isUpdating) return;
                if (socketDropdown.SelectedItem is TrackSocket selectedSocket)
                {
                    byte socketNum = (byte)selectedSocket;
                    // Update low nibble of low byte
                    byte lo = (byte)(Value & 0xFF);
                    byte hi = (byte)(Value >> 8);
                    lo = (byte)((lo & 0xF0) | (socketNum & 0x0F));
                    UpdateInternal((ushort)((hi << 8) | lo));
                }
            };

            cappedCheckBox.IsCheckedChanged += (s, e) =>
            {
                if (isUpdating) return;
                bool isCapped = cappedCheckBox.IsChecked == true;
                byte lo = (byte)(Value & 0xFF);
                byte hi = (byte)(Value >> 8);
                byte highNibble = isCapped ? (byte)0x70 : (byte)0x80;
                lo = (byte)((lo & 0x0F) | highNibble);
                UpdateInternal((ushort)((hi << 8) | lo));
            };

            hexTextBox.TextChanged += (s, e) =>
            {
                if (isUpdating) return;
                string text = hexTextBox.Text ?? string.Empty;
                text = text.Trim();
                if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Substring(2);
                }

                if (ushort.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort parsed))
                {
                    UpdateInternal(parsed);
                }
            };
        }

        public void SetValue(ushort value)
        {
            UpdateInternal(value, refreshHex: true);
        }

        private void UpdateInternal(ushort newValue, bool refreshHex = true)
        {
            Value = newValue;
            isUpdating = true;

            try
            {
                byte lo = (byte)(newValue & 0xFF);
                int socketNum = lo & 0x0F;
                bool isCapped = (lo & 0xF0) == 0x70;

                if (socketNum >= 0 && socketNum <= 7)
                {
                    socketDropdown.SelectedItem = (TrackSocket)socketNum;
                }
                else
                {
                    socketDropdown.SelectedIndex = -1;
                }

                cappedCheckBox.IsChecked = isCapped;

                if (refreshHex)
                {
                    hexTextBox.Text = $"0x{newValue:X4}";
                }
            }
            finally
            {
                isUpdating = false;
            }

            ValueChanged?.Invoke(Value);
        }
    }
}
