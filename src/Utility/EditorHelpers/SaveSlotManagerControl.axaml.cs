using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using SMM2SaveEditor.Utility;

namespace SMM2SaveEditor.Utility.EditorHelpers
{
    public partial class SaveSlotManagerControl : UserControl
    {
        public event Action<string>? RequestOpenLevel;
        public event Func<Level?>? RequestCurrentLevel;

        private List<SlotInfo> slots = new();

        public SaveSlotManagerControl()
        {
            InitializeComponent();

            SlotsListBox.SelectionChanged += (s, e) => UpdateInspector();

            string defaultPath = SaveManagerService.DetectSaveDirectory();
            if (!string.IsNullOrEmpty(defaultPath))
            {
                SavePathBox.Text = defaultPath;
                RefreshSlots();
            }
        }

        private void OnBrowseFolder(object? sender, RoutedEventArgs e)
        {
            BrowseFolderAsync();
        }

        private async void BrowseFolderAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Ryujinx / Yuzu SMM2 Save Folder",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                SavePathBox.Text = folders[0].Path.LocalPath;
                RefreshSlots();
            }
        }

        private void OnRefresh(object? sender, RoutedEventArgs e)
        {
            RefreshSlots();
        }

        public void RefreshSlots()
        {
            string path = SavePathBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                StatusMessage.Text = "Please specify a valid save directory.";
                return;
            }

            try
            {
                var targets = SaveManagerService.GetTargetDirectories(path);
                if (targets.Count > 1)
                {
                    DetectInfoText.Text = $"Double-buffer active ({targets.Count} subfolders). Syncs to '0' and '1'.";
                }
                else
                {
                    DetectInfoText.Text = $"Single save directory: {path}";
                }

                int prevSelectedIndex = SlotsListBox.SelectedIndex;
                slots = SaveManagerService.ScanSlots(path, 60);
                SlotsListBox.ItemsSource = slots;

                if (prevSelectedIndex >= 0 && prevSelectedIndex < slots.Count)
                {
                    SlotsListBox.SelectedIndex = prevSelectedIndex;
                }
                else if (slots.Count > 0)
                {
                    SlotsListBox.SelectedIndex = 0;
                }

                int active = slots.Count(s => s.Exists && !s.IsHiddenInCoursebot && !s.IsCorrupted);
                int hidden = slots.Count(s => s.IsHiddenInCoursebot);
                int corrupted = slots.Count(s => s.IsCorrupted);
                int withThumbs = slots.Count(s => s.HasThumbnail);

                string hiddenNotice = hidden > 0 ? $", ⚠️ {hidden} hidden in Coursebot" : "";
                string corruptedNotice = corrupted > 0 ? $", ❌ {corrupted} corrupted" : "";
                StatusMessage.Text = $"Found {active} active course(s){hiddenNotice}{corruptedNotice}, {withThumbs} with thumbnails.";

                UpdateInspector();
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Error reading slots: {ex.Message}";
            }
        }

        private SlotInfo? GetSelectedSlot()
        {
            return SlotsListBox.SelectedItem as SlotInfo;
        }

        private void UpdateInspector()
        {
            var selected = GetSelectedSlot();
            if (selected == null)
            {
                InspectorInGameSlot.Text = "-";
                InspectorDisplayName.Text = "No Slot Selected";
                InspectorGameStyle.Text = "-";
                InspectorCourseTitle.Text = "Select a slot from the grid";

                InspectorHealthPill.Background = Brush.Parse("#242735");
                InspectorHealthPill.BorderBrush = Brush.Parse("#32364A");
                InspectorHealthText.Text = "⚪ None";
                InspectorHealthText.Foreground = Brush.Parse("#6C7284");
                InspectorHealthDetail.Text = "Select a course slot to inspect its properties and actions.";

                InspectorUnhideBtn.IsVisible = false;
                InspectorRepairThumbBtn.IsVisible = false;

                InspectorThumbImage.Source = null;
                InspectorThumbImage.IsVisible = false;
                InspectorNoThumbText.IsVisible = true;
                InspectorThumbStatus.Text = "No thumbnail";

                InspectorOpenEditorBtn.IsEnabled = false;
                InspectorExportBcdBtn.IsEnabled = false;
                return;
            }

            InspectorInGameSlot.Text = selected.InGameSlot;
            InspectorDisplayName.Text = selected.DisplayName;
            InspectorGameStyle.Text = string.IsNullOrEmpty(selected.GameStyle) ? "-" : selected.GameStyle;
            InspectorCourseTitle.Text = selected.Title;

            try
            {
                InspectorHealthPill.Background = Brush.Parse(selected.HealthBadgeBackground);
                InspectorHealthPill.BorderBrush = Brush.Parse(selected.HealthBadgeBorder);
                InspectorHealthText.Text = selected.HealthBadgeText;
                InspectorHealthText.Foreground = Brush.Parse(selected.HealthBadgeForeground);
            }
            catch { }

            if (selected.IsHiddenInCoursebot)
            {
                InspectorHealthDetail.Text = "⚠️ This course file is valid, but hidden in SMM2 Coursebot because save.dat marks this slot as inactive (status = 0). Click 'Unhide in Coursebot' to restore it!";
            }
            else if (selected.IsCorrupted)
            {
                InspectorHealthDetail.Text = $"❌ Integrity check failed: {selected.PrimaryCorruptionReason}";
            }
            else if (selected.Exists)
            {
                InspectorHealthDetail.Text = "🟢 Course and save registration are fully healthy and visible in Coursebot.";
            }
            else
            {
                InspectorHealthDetail.Text = "⚪ This slot is currently empty and available for new courses.";
            }

            InspectorUnhideBtn.IsVisible = selected.CanUnhide;
            InspectorRepairThumbBtn.IsVisible = selected.CanRepairThumbnail;

            if (selected.ThumbnailBitmap != null)
            {
                InspectorThumbImage.Source = selected.ThumbnailBitmap;
                InspectorThumbImage.IsVisible = true;
                InspectorNoThumbText.IsVisible = false;
                InspectorThumbStatus.Text = $"course_thumb_{selected.SlotId}.btl (Active)";
            }
            else
            {
                InspectorThumbImage.Source = null;
                InspectorThumbImage.IsVisible = false;
                InspectorNoThumbText.IsVisible = true;
                InspectorThumbStatus.Text = selected.HasThumbnail ? "Blank / Empty Container" : "No thumbnail file";
            }

            InspectorOpenEditorBtn.IsEnabled = selected.Exists && !selected.IsCorrupted;
            InspectorExportBcdBtn.IsEnabled = selected.Exists;
        }

        private void OnSlotDoubleTapped(object? sender, TappedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected != null && selected.Exists && !selected.IsCorrupted)
            {
                RequestOpenLevel?.Invoke(selected.FilePath);
            }
        }

        private void OnOpenInEditorClick(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected != null && selected.Exists && !selected.IsCorrupted)
            {
                RequestOpenLevel?.Invoke(selected.FilePath);
            }
        }

        private void OnUnhideClick(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null || !selected.CanUnhide) return;

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir)) return;

            bool ok = SaveDataCrypto.SetSlotStatus(saveDir, selected.SlotIndex, occupied: true);
            if (ok)
            {
                StatusMessage.Text = $"Successfully unhidden {selected.DisplayName} in Coursebot! It is now active and visible in-game.";
                RefreshSlots();
            }
            else
            {
                StatusMessage.Text = $"Failed to unhide {selected.DisplayName}.";
            }
        }

        private async void OnOpenDiagnosticsClick(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null) return;

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            var topLevel = TopLevel.GetTopLevel(this) as Window;
            if (topLevel == null) return;

            var diagWindow = new CourseDiagnosticsWindow(selected, saveDir, RefreshSlots);
            await diagWindow.ShowDialog(topLevel);
        }

        private void OnRepairThumbClick(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null) return;

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir)) return;

            string thumbFile = $"course_thumb_{selected.SlotIndex:D3}.btl";
            var targets = SaveManagerService.GetTargetDirectories(saveDir);
            byte[]? btlBytes = null;
            foreach (var t in targets)
            {
                string p = Path.Combine(t, thumbFile);
                if (File.Exists(p)) { btlBytes = File.ReadAllBytes(p); break; }
            }

            if (btlBytes == null) return;

            try
            {
                byte[] jpegBytes = ThumbnailCrypto.DecryptThumbnail(btlBytes);
                byte[] repairedBtl = jpegBytes != null && jpegBytes.Length > 0
                    ? ThumbnailCrypto.EncryptThumbnail(jpegBytes)
                    : ThumbnailCrypto.EncryptThumbnail(Array.Empty<byte>());

                SaveManagerService.WriteThumbnail(saveDir, selected.SlotIndex, repairedBtl);
                StatusMessage.Text = $"Repaired thumbnail signature for {selected.DisplayName}.";
                RefreshSlots();
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Failed to repair thumbnail: {ex.Message}";
            }
        }

        private async void OnImportImageClick(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null) return;

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir)) return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) return;

            var picked = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = $"Select Image for {selected.DisplayName} ({selected.InGameSlot})",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Image Files (*.png, *.jpg, *.jpeg, *.webp, *.bmp)")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp" }
                    },
                    new FilePickerFileType("All Files (*.*)") { Patterns = new[] { "*.*" } }
                }
            });

            if (picked.Count == 0) return;

            try
            {
                StatusMessage.Text = $"Converting '{picked[0].Name}' to encrypted Nintendo .btl thumbnail...";
                using var ms = new MemoryStream();
                await using (var fs = await picked[0].OpenReadAsync())
                {
                    await fs.CopyToAsync(ms);
                }
                ms.Position = 0;

                byte[] encryptedBtl = ThumbnailCrypto.ConvertImageToBtl(ms);
                SaveManagerService.WriteThumbnail(saveDir, selected.SlotIndex, encryptedBtl);

                StatusMessage.Text = $"Converted & injected thumbnail '{picked[0].Name}' into {selected.DisplayName}!";
                RefreshSlots();
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Failed to import image: {ex.Message}";
            }
        }

        private async void OnExportImageClick(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null || !selected.HasThumbnail) return;

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            var targets = SaveManagerService.GetTargetDirectories(saveDir);
            string srcFile = $"course_thumb_{selected.SlotIndex:D3}.btl";
            string? foundPath = null;
            foreach (var t in targets)
            {
                string p = Path.Combine(t, srcFile);
                if (File.Exists(p)) { foundPath = p; break; }
            }

            if (foundPath == null) return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) return;

            try
            {
                byte[] btlBytes = await File.ReadAllBytesAsync(foundPath);
                byte[] jpegBytes = ThumbnailCrypto.DecryptThumbnail(btlBytes);

                if (jpegBytes == null || jpegBytes.Length == 0)
                {
                    StatusMessage.Text = "Thumbnail is blank or empty.";
                    return;
                }

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = $"Export Thumbnail Image for {selected.DisplayName}",
                    SuggestedFileName = $"course_thumb_{selected.SlotId}.jpg",
                    DefaultExtension = "jpg",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("JPEG Image (*.jpg)") { Patterns = new[] { "*.jpg", "*.jpeg" } }
                    }
                });

                if (file == null) return;

                await File.WriteAllBytesAsync(file.Path.LocalPath, jpegBytes);
                StatusMessage.Text = $"Exported thumbnail image to '{file.Name}'.";
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Failed to export image: {ex.Message}";
            }
        }

        private async void OnImportBtlClick(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null) return;

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir)) return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) return;

            var picked = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = $"Select Thumbnail (.btl) for {selected.DisplayName}",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("SMM2 Course Thumbnail (.btl)") { Patterns = new[] { "*.btl" } }
                }
            });

            if (picked.Count == 0) return;

            try
            {
                byte[] btlBytes = await File.ReadAllBytesAsync(picked[0].Path.LocalPath);
                SaveManagerService.WriteThumbnail(saveDir, selected.SlotIndex, btlBytes);
                StatusMessage.Text = $"Imported thumbnail '{picked[0].Name}' into {selected.DisplayName}!";
                RefreshSlots();
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Failed to import thumbnail: {ex.Message}";
            }
        }

        private async void OnExportBtlClick(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null || !selected.HasThumbnail) return;

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            var targets = SaveManagerService.GetTargetDirectories(saveDir);
            string srcFile = $"course_thumb_{selected.SlotIndex:D3}.btl";
            string? foundPath = null;
            foreach (var t in targets)
            {
                string p = Path.Combine(t, srcFile);
                if (File.Exists(p)) { foundPath = p; break; }
            }

            if (foundPath == null) return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = $"Export Thumbnail for {selected.DisplayName}",
                DefaultExtension = $"course_thumb_{selected.SlotId}.btl",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("SMM2 Course Thumbnail (.btl)") { Patterns = new[] { "*.btl" } }
                }
            });

            if (file == null) return;

            try
            {
                byte[] btlBytes = await File.ReadAllBytesAsync(foundPath);
                await File.WriteAllBytesAsync(file.Path.LocalPath, btlBytes);
                StatusMessage.Text = $"Exported thumbnail to '{file.Name}'.";
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Failed to export thumbnail: {ex.Message}";
            }
        }

        private async void OnCloneThumbClick(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null) return;

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir)) return;

            var availableSlots = slots.Where(s => s.HasThumbnail && s.SlotIndex != selected.SlotIndex).ToList();
            if (availableSlots.Count == 0)
            {
                StatusMessage.Text = "No other slots with thumbnails found to clone from.";
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this) as Window;
            if (topLevel == null) return;

            var dialog = new Window
            {
                Title = $"Clone Thumbnail into {selected.DisplayName}",
                Width = 440,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = (IBrush)this.FindResource("AppBackgroundBrush")!
            };

            var stack = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
            stack.Children.Add(new TextBlock 
            { 
                Text = $"Select a source slot to copy thumbnail into {selected.DisplayName} ({selected.InGameSlot}):", 
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = (IBrush)this.FindResource("TextPrimaryBrush")!
            });

            var combo = new ComboBox 
            { 
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                ItemsSource = availableSlots.Select(s => $"{s.DisplayName} ({s.InGameSlot}) - {s.Title}").ToList(),
                SelectedIndex = 0
            };
            stack.Children.Add(combo);

            var btnStack = new StackPanel 
            { 
                Orientation = Avalonia.Layout.Orientation.Horizontal, 
                Spacing = 10, 
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Avalonia.Thickness(0, 16, 0, 0)
            };

            var applyBtn = new Button { Content = "Clone Thumbnail", Classes = { "accent" } };
            var cancelBtn = new Button { Content = "Cancel", Classes = { "toolbarBtn" } };
            btnStack.Children.Add(applyBtn);
            btnStack.Children.Add(cancelBtn);
            stack.Children.Add(btnStack);
            dialog.Content = stack;

            applyBtn.Click += (s, ev) =>
            {
                if (combo.SelectedIndex >= 0 && combo.SelectedIndex < availableSlots.Count)
                {
                    int srcSlot = availableSlots[combo.SelectedIndex].SlotIndex;
                    bool ok = SaveManagerService.CopyThumbnail(saveDir, srcSlot, selected.SlotIndex);
                    if (ok)
                    {
                        StatusMessage.Text = $"Cloned thumbnail from Slot {srcSlot:D3} into {selected.DisplayName}!";
                        RefreshSlots();
                    }
                }
                dialog.Close();
            };

            cancelBtn.Click += (s, ev) => dialog.Close();
            await dialog.ShowDialog(topLevel);
        }

        private void OnDeleteThumbClick(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null || !selected.HasThumbnail) return;

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            SaveManagerService.DeleteThumbnail(saveDir, selected.SlotIndex);
            StatusMessage.Text = $"Deleted thumbnail from {selected.DisplayName}.";
            RefreshSlots();
        }

        private async void OnImportBcdClick(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null) return;

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir)) return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) return;

            var picked = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = $"Import .bcd file into {selected.DisplayName}",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Mario Maker 2 Course (.bcd)") { Patterns = new[] { "*.bcd" } }
                }
            });

            if (picked.Count == 0) return;

            try
            {
                string bcdPath = picked[0].Path.LocalPath;
                byte[] bcdBytes = await File.ReadAllBytesAsync(bcdPath);

                byte[] decrypted;
                if (bcdBytes.Length == 0x5C000)
                {
                    decrypted = LevelCrypto.DecryptLevel(bcdBytes);
                }
                else if (bcdBytes.Length == 0x5BFD0 - 0x10)
                {
                    decrypted = bcdBytes;
                }
                else if (bcdBytes.Length == 0x5BFD0)
                {
                    decrypted = bcdBytes.Skip(0x10).ToArray();
                }
                else
                {
                    throw new ArgumentException($"Invalid .bcd file length: {bcdBytes.Length}");
                }

                // Randomize CreationID (offset 0x24 in decrypted payload)
                byte[] idBytes = new byte[4];
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    rng.GetBytes(idBytes);
                }
                Buffer.BlockCopy(idBytes, 0, decrypted, 0x24, 4);

                bcdBytes = LevelCrypto.EncryptLevel(decrypted);

                byte[]? btlBytes = null;
                string bcdDir = Path.GetDirectoryName(bcdPath) ?? "";
                string bcdBase = Path.GetFileNameWithoutExtension(bcdPath);
                string btlSameName = Path.Combine(bcdDir, bcdBase + ".btl");
                string btlThumbName = Path.Combine(bcdDir, bcdBase.Replace("course_data", "course_thumb") + ".btl");

                if (File.Exists(btlSameName))
                {
                    btlBytes = await File.ReadAllBytesAsync(btlSameName);
                }
                else if (File.Exists(btlThumbName))
                {
                    btlBytes = await File.ReadAllBytesAsync(btlThumbName);
                }

                SaveManagerService.WriteSlot(saveDir, selected.SlotIndex, bcdBytes, btlBytes);
                string extra = btlBytes != null ? " (with matching thumbnail)" : "";
                StatusMessage.Text = $"Imported '{picked[0].Name}'{extra} into {selected.DisplayName}!";
                RefreshSlots();
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Failed to import file: {ex.Message}";
            }
        }

        private async void OnExportBcdClick(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null || !selected.Exists) return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = $"Export {selected.DisplayName} to .bcd",
                DefaultExtension = $"{selected.Title.Replace(' ', '_')}_{selected.SlotId}.bcd",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Mario Maker 2 Course (.bcd)") { Patterns = new[] { "*.bcd" } }
                }
            });

            if (file == null) return;

            try
            {
                byte[] bcdBytes = await File.ReadAllBytesAsync(selected.FilePath);
                await File.WriteAllBytesAsync(file.Path.LocalPath, bcdBytes);
                StatusMessage.Text = $"Exported to '{file.Name}'.";
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Failed to export: {ex.Message}";
            }
        }

        private void OnInjectLevelClick(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null) return;

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir))
            {
                StatusMessage.Text = "Save directory does not exist.";
                return;
            }

            var currentLevel = RequestCurrentLevel?.Invoke();
            if (currentLevel == null)
            {
                StatusMessage.Text = "No level currently opened in editor to inject.";
                return;
            }

            try
            {
                currentLevel.RegenerateCreationId();
                byte[] raw = currentLevel.GetBytes();
                byte[] encrypted = LevelCrypto.EncryptLevel(raw);

                SaveManagerService.WriteSlot(saveDir, selected.SlotIndex, encrypted);
                StatusMessage.Text = $"Successfully injected current level into {selected.DisplayName}!";
                RefreshSlots();
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Failed to inject level: {ex.Message}";
            }
        }

        private async void OnBackupSave(object? sender, RoutedEventArgs e)
        {
            string saveDir = SavePathBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir)) return;

            try
            {
                string zipPath = SaveManagerService.BackupSaveDirectory(saveDir);
                StatusMessage.Text = $"Backup created: {Path.GetFileName(zipPath)}";
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Backup failed: {ex.Message}";
            }
        }
    }
}
