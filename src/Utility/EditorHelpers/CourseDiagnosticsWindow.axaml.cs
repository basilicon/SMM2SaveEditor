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
        private readonly Level? currentLevel;
        private readonly string? courseFilePath;

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

        public CourseDiagnosticsWindow(Level level, string? courseFilePath, Action? onStateChanged = null)
        {
            this.currentLevel = level;
            this.courseFilePath = courseFilePath;
            this.onStateChanged = onStateChanged ?? (() => { });
            this.slot = new SlotInfo
            {
                Title = !string.IsNullOrWhiteSpace(level.levelName) ? level.levelName : "Active Level",
                GameStyle = level.gameStyle.ToString()
            };
            this.saveDir = "";

            InitializeComponent();
            PopulateDiagnostics();
        }

        private void PopulateDiagnostics()
        {
            CourseHealthReport report;
            if (currentLevel != null)
            {
                SlotTitleText.Text = $"Level: {slot.Title} ({slot.GameStyle})";
                report = CourseDiagnostics.DiagnoseLevel(currentLevel, courseFilePath);
                UnhideButton.IsVisible = false;
                RepairThumbButton.IsVisible = false;
            }
            else
            {
                SlotTitleText.Text = $"{slot.DisplayName} ({slot.InGameSlot}): {slot.Title}";
                report = slot.HealthReport ?? CourseDiagnostics.DiagnoseSlot(saveDir, slot.SlotIndex, slot.IsOccupiedInSave);
                UnhideButton.IsVisible = report.CanUnhide;
                RepairThumbButton.IsVisible = report.CanRepairThumbnail;
            }

            ChecksList.ItemsSource = report.Checks;
            SanitizeFlagsButton.IsVisible = report.CanSanitizeFlags;

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
            if (currentLevel != null)
            {
                bool ok = CourseDiagnostics.RepairLevel(currentLevel);
                if (!string.IsNullOrEmpty(courseFilePath) && File.Exists(courseFilePath))
                {
                    try
                    {
                        byte[] encrypted = LevelCrypto.EncryptLevel(currentLevel.GetBytes());
                        File.WriteAllBytes(courseFilePath, encrypted);
                    }
                    catch { }
                }
                onStateChanged?.Invoke();
                PopulateDiagnostics();
                return;
            }

            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir)) return;

            bool success = CourseDiagnostics.RepairCourse(saveDir, slot.SlotIndex);
            if (success)
            {
                onStateChanged?.Invoke();
                slot.HealthReport = CourseDiagnostics.DiagnoseSlot(saveDir, slot.SlotIndex, true);
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

            try
            {
                byte[] repairedBtl;
                if (btlBytes != null)
                {
                    byte[]? jpegBytes = null;
                    try { jpegBytes = ThumbnailCrypto.DecryptThumbnail(btlBytes); } catch { }

                    if (jpegBytes != null && jpegBytes.Length >= 2 && jpegBytes[0] == 0xFF && jpegBytes[1] == 0xD8)
                    {
                        repairedBtl = ThumbnailCrypto.EncryptThumbnail(jpegBytes);
                    }
                    else
                    {
                        repairedBtl = ThumbnailCrypto.GeneratePlaceholderThumbnailBtl();
                    }
                }
                else
                {
                    repairedBtl = ThumbnailCrypto.GeneratePlaceholderThumbnailBtl();
                }

                SaveManagerService.WriteThumbnail(saveDir, slot.SlotIndex, repairedBtl);
                onStateChanged?.Invoke();
                slot.HealthReport = CourseDiagnostics.DiagnoseSlot(saveDir, slot.SlotIndex, slot.IsOccupiedInSave);
                PopulateDiagnostics();
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
