using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using SMM2SaveEditor.Utility;

namespace SMM2SaveEditor.Utility.EditorHelpers
{
    public partial class CourseDiagnosticsWindow : Window
    {
        private readonly SlotInfo slot;
        private readonly string saveDir;
        private readonly Action onStateChanged;

        public CourseDiagnosticsWindow() : this(new SlotInfo(), "", () => { })
        {
        }

        public CourseDiagnosticsWindow(SlotInfo slot, string saveDir, Action onStateChanged)
        {
            this.slot = slot;
            this.saveDir = saveDir;
            this.onStateChanged = onStateChanged;

            InitializeComponent();
            PopulateDiagnostics();
        }

        private void PopulateDiagnostics()
        {
            SlotTitleText.Text = $"{slot.DisplayName} ({slot.InGameSlot}): {slot.Title}";

            var report = slot.HealthReport ?? CourseDiagnostics.DiagnoseSlot(saveDir, slot.SlotIndex, slot.IsOccupiedInSave);
            ChecksList.ItemsSource = report.Checks;

            UnhideButton.IsVisible = report.CanUnhide;
            SanitizeFlagsButton.IsVisible = report.CanSanitizeFlags;
            RepairThumbButton.IsVisible = report.CanRepairThumbnail;

            switch (report.Status)
            {
                case SlotHealthStatus.Healthy:
                    StatusBanner.Background = Brush.Parse("#13261B");
                    StatusBanner.BorderBrush = Brush.Parse("#27AE60");
                    StatusHeadingText.Text = "🟢 Fully Healthy & Active in Coursebot";
                    StatusHeadingText.Foreground = Brush.Parse("#2ECC71");
                    StatusPill.Background = Brush.Parse("#193B26");
                    StatusPill.BorderBrush = Brush.Parse("#27AE60");
                    StatusPillText.Text = "🟢 Active";
                    StatusPillText.Foreground = Brush.Parse("#2ECC71");
                    StatusExplanationText.Text = "This course passes all SMM2 cryptographic signatures (CMAC/HMAC), header checksums, entity limits, and is registered active in save.dat. It will load and display without issues in Coursebot.";
                    break;

                case SlotHealthStatus.HiddenInCoursebot:
                    StatusBanner.Background = Brush.Parse("#2A1F10");
                    StatusBanner.BorderBrush = Brush.Parse("#E67E22");
                    StatusHeadingText.Text = "⚠️ Course Data Valid — Hidden in Coursebot";
                    StatusHeadingText.Foreground = Brush.Parse("#F39C12");
                    StatusPill.Background = Brush.Parse("#4A3210");
                    StatusPill.BorderBrush = Brush.Parse("#E67E22");
                    StatusPillText.Text = "⚠️ Hidden in Coursebot";
                    StatusPillText.Foreground = Brush.Parse("#F39C12");
                    StatusExplanationText.Text = "The course file on disk is completely valid and decryptable, but save.dat marks this slot as inactive (status = 0). SMM2 Coursebot explicitly checks this byte and skips loading the slot! Click 'Unhide in Coursebot' below to restore it in-game.";
                    break;

                case SlotHealthStatus.Corrupted:
                    StatusBanner.Background = Brush.Parse("#2E1313");
                    StatusBanner.BorderBrush = Brush.Parse("#C0392B");
                    StatusHeadingText.Text = "❌ Integrity or Signature Corruption Detected";
                    StatusHeadingText.Foreground = Brush.Parse("#E74C3C");
                    StatusPill.Background = Brush.Parse("#421818");
                    StatusPill.BorderBrush = Brush.Parse("#C0392B");
                    StatusPillText.Text = "❌ Corrupted";
                    StatusPillText.Foreground = Brush.Parse("#E74C3C");
                    StatusExplanationText.Text = $"Corruption Detected: {report.PrimaryReason}\nSMM2 checks these exact signatures on boot. If any check fails, the game resets or hides the slot to prevent engine crashes.";
                    break;

                default:
                    StatusBanner.Background = Brush.Parse("#1A1C24");
                    StatusBanner.BorderBrush = Brush.Parse("#2E3346");
                    StatusHeadingText.Text = "⚪ Empty Course Slot";
                    StatusHeadingText.Foreground = Brush.Parse("#8A92A6");
                    StatusPill.Background = Brush.Parse("#242735");
                    StatusPill.BorderBrush = Brush.Parse("#32364A");
                    StatusPillText.Text = "⚪ Empty";
                    StatusPillText.Foreground = Brush.Parse("#6C7284");
                    StatusExplanationText.Text = "No course data is stored in this slot.";
                    break;
            }
        }

        private void OnUnhideClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir)) return;

            bool ok = SaveDataCrypto.SetSlotStatus(saveDir, slot.SlotIndex, occupied: true);
            if (ok)
            {
                onStateChanged?.Invoke();
                Close();
            }
        }

        private void OnSanitizeFlagsClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir)) return;

            bool ok = CourseDiagnostics.SanitizeCourseFlags(saveDir, slot.SlotIndex);
            if (ok)
            {
                onStateChanged?.Invoke();
                slot.HealthReport = CourseDiagnostics.DiagnoseSlot(saveDir, slot.SlotIndex, slot.IsOccupiedInSave);
                PopulateDiagnostics();
            }
        }

        private void OnRepairThumbClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir)) return;

            string thumbFile = $"course_thumb_{slot.SlotIndex:D3}.btl";
            var targets = SaveManagerService.GetTargetDirectories(saveDir);
            byte[]? btlBytes = null;
            foreach (var t in targets)
            {
                string p = Path.Combine(t, thumbFile);
                if (File.Exists(p))
                {
                    btlBytes = File.ReadAllBytes(p);
                    break;
                }
            }

            if (btlBytes == null) return;

            try
            {
                byte[] jpegBytes = ThumbnailCrypto.DecryptThumbnail(btlBytes);
                byte[] repairedBtl;
                if (jpegBytes != null && jpegBytes.Length > 0)
                {
                    repairedBtl = ThumbnailCrypto.EncryptThumbnail(jpegBytes);
                }
                else
                {
                    repairedBtl = ThumbnailCrypto.EncryptThumbnail(Array.Empty<byte>());
                }

                SaveManagerService.WriteThumbnail(saveDir, slot.SlotIndex, repairedBtl);
                onStateChanged?.Invoke();
                Close();
            }
            catch (Exception ex)
            {
                StatusExplanationText.Text = $"Failed to repair thumbnail: {ex.Message}";
            }
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
