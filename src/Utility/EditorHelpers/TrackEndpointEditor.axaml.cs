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
        private CheckBox cutCheckBox;
        private TextBox hexTextBox;

        public ushort Value { get; private set; }
        private bool isUpdating = false;

        public TrackEndpointEditor()
        {
            InitializeComponent();

            socketDropdown = this.FindControl<ComboBox>("SocketDropdown")!;
            cappedCheckBox = this.FindControl<CheckBox>("CappedCheckBox")!;
            cutCheckBox = this.FindControl<CheckBox>("CutCheckBox")!;
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

            cutCheckBox.IsCheckedChanged += (sender, e) =>
            {
                if (isUpdating) return;
                if (cutCheckBox.IsChecked == true)
                {
                    UpdateInternal(0x0104);
                }
                else
                {
                    byte socketNum = socketDropdown.SelectedItem is TrackSocket sock ? (byte)sock : (byte)0;
                    UpdateInternal((ushort)(0x0080 | socketNum));
                }
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

        public void SetValue(ushort value, TrackType trackType = TrackType.horizontal, int endpointIndex = 1)
        {
            int typeId = (int)trackType;
            TrackSocket defaultPort = TrackSocket.East;
            if (typeId >= 0 && typeId < Entities.Track.TrackPorts.Length)
            {
                var ports = Entities.Track.TrackPorts[typeId];
                defaultPort = endpointIndex == 1 ? ports.port1 : ports.port2;
            }

            TrackSocket displaySocket = Entities.Track.ResolveEndpointSocket(value, defaultPort);

            UpdateInternal(value, displaySocket, refreshHex: true);
        }

        private void UpdateInternal(ushort newValue, TrackSocket? displaySocket = null, bool refreshHex = true)
        {
            Value = newValue;
            isUpdating = true;

            try
            {
                bool isCut = newValue == 0x0104;
                cutCheckBox.IsChecked = isCut;
                socketDropdown.IsEnabled = !isCut;
                cappedCheckBox.IsEnabled = !isCut;

                byte lo = (byte)(newValue & 0xFF);
                bool isCapped = !isCut && ((lo & 0xF0) == 0x70 || (newValue >= 0x0070 && newValue <= 0x0077));

                if (isCut)
                {
                    socketDropdown.SelectedIndex = -1;
                    cappedCheckBox.IsChecked = false;
                }
                else
                {
                    if (displaySocket != null)
                    {
                        socketDropdown.SelectedItem = displaySocket.Value;
                    }
                    else if ((lo & 0x0F) <= 7)
                    {
                        socketDropdown.SelectedItem = (TrackSocket)(lo & 0x0F);
                    }
                    else
                    {
                        socketDropdown.SelectedIndex = -1;
                    }

                    cappedCheckBox.IsChecked = isCapped;
                }

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
