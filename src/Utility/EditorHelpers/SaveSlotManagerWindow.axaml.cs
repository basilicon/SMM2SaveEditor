using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using SMM2SaveEditor.Utility;

namespace SMM2SaveEditor.Utility.EditorHelpers
{
    public partial class SaveSlotManagerWindow : Window
    {
        private readonly MainWindow mainWindow;
        private List<SlotInfo> slots = new();

        public SaveSlotManagerWindow() : this(null!)
        {
        }

        public SaveSlotManagerWindow(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            InitializeComponent();

            SlotsListBox.SelectionChanged += (s, e) => UpdateSelectedThumbStatus();

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
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
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

        private void RefreshSlots()
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
                    DetectInfoText.Text = $"Double-buffer active ({targets.Count} subfolders detected). All saves sync to both folders automatically.";
                }
                else
                {
                    DetectInfoText.Text = $"Single save directory active: {path}";
                }

                int prevSelectedIndex = SlotsListBox.SelectedIndex;
                slots = SaveManagerService.ScanSlots(path, 60);
                SlotsListBox.ItemsSource = slots;

                if (prevSelectedIndex >= 0 && prevSelectedIndex < slots.Count)
                {
                    SlotsListBox.SelectedIndex = prevSelectedIndex;
                }

                int occupied = slots.Count(s => s.Exists);
                int withThumbs = slots.Count(s => s.HasThumbnail);
                StatusMessage.Text = $"Found {occupied} course slot(s), {withThumbs} with thumbnails.";
                UpdateSelectedThumbStatus();
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

        private void UpdateSelectedThumbStatus()
        {
            var selected = GetSelectedSlot();
            if (selected == null)
            {
                SelectedThumbStatusText.Text = "Select a slot to modify its thumbnail.";
                return;
            }

            if (selected.HasThumbnail)
            {
                SelectedThumbStatusText.Text = $"{selected.DisplayName} ({selected.InGameSlot}): Thumbnail active (course_thumb_{selected.SlotId}.btl)";
            }
            else
            {
                SelectedThumbStatusText.Text = $"{selected.DisplayName} ({selected.InGameSlot}): No thumbnail assigned.";
            }
        }

        private void OnLoadSlot(object? sender, RoutedEventArgs e)
        {
            LoadSelectedSlot();
        }

        private void OnSlotDoubleTapped(object? sender, TappedEventArgs e)
        {
            LoadSelectedSlot();
        }

        private void LoadSelectedSlot()
        {
            var selected = GetSelectedSlot();
            if (selected == null)
            {
                StatusMessage.Text = "Please select a slot from the list.";
                return;
            }

            if (!selected.Exists || string.IsNullOrEmpty(selected.FilePath) || !File.Exists(selected.FilePath))
            {
                StatusMessage.Text = $"{selected.DisplayName} is empty.";
                return;
            }

            if (mainWindow != null)
            {
                mainWindow.LoadFromFile(selected.FilePath);
                StatusMessage.Text = $"Loaded {selected.DisplayName} ('{selected.Title}') into editor.";
                Close();
            }
        }

        private void OnInjectLevel(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null)
            {
                StatusMessage.Text = "Please select a slot to inject into.";
                return;
            }

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir))
            {
                StatusMessage.Text = "Save directory does not exist.";
                return;
            }

            if (mainWindow == null || mainWindow.Level == null)
            {
                StatusMessage.Text = "No level currently opened in editor.";
                return;
            }

            try
            {
                // Ensure injected level has a unique CreationID to prevent Coursebot collisions
                mainWindow.Level.RegenerateCreationId();
                byte[] raw = mainWindow.Level.GetBytes();
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

        private async void OnImportBcd(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null)
            {
                StatusMessage.Text = "Please select a slot to import into.";
                return;
            }

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir))
            {
                StatusMessage.Text = "Save directory does not exist.";
                return;
            }

            var picked = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = $"Import .bcd file into {selected.DisplayName}",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Mario Maker 2 Course (.bcd)")
                    {
                        Patterns = new[] { "*.bcd" }
                    }
                }
            });

            if (picked.Count == 0) return;

            try
            {
                string bcdPath = picked[0].Path.LocalPath;
                byte[] bcdBytes = await File.ReadAllBytesAsync(bcdPath);

                // Decrypt payload to assign a unique CreationID so Coursebot does not flag duplicates
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

                // Check if matching .btl file exists next to the .bcd
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
                StatusMessage.Text = $"Successfully imported '{picked[0].Name}'{extra} into {selected.DisplayName}!";
                RefreshSlots();
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Failed to import file: {ex.Message}";
            }
        }

        private async void OnExportBcd(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null || !selected.Exists)
            {
                StatusMessage.Text = "Please select an existing course slot to export.";
                return;
            }

            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = $"Export {selected.DisplayName} to .bcd",
                DefaultExtension = $"{selected.Title.Replace(' ', '_')}_{selected.SlotId}.bcd",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Mario Maker 2 Course (.bcd)")
                    {
                        Patterns = new[] { "*.bcd" }
                    }
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
        private async void OnImportImage(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null)
            {
                StatusMessage.Text = "Please select a slot to import an image for.";
                return;
            }

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir))
            {
                StatusMessage.Text = "Save directory does not exist.";
                return;
            }

            var picked = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = $"Select Image for {selected.DisplayName} ({selected.InGameSlot})",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Image Files (*.png, *.jpg, *.jpeg, *.webp, *.bmp)")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp" }
                    },
                    new FilePickerFileType("All Files (*.*)")
                    {
                        Patterns = new[] { "*.*" }
                    }
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

                StatusMessage.Text = $"Successfully converted & injected thumbnail '{picked[0].Name}' into {selected.DisplayName} ({selected.InGameSlot})!";
                RefreshSlots();
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Failed to convert/import image: {ex.Message}";
            }
        }

        private async void OnImportBtl(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null)
            {
                StatusMessage.Text = "Please select a slot to import a thumbnail into.";
                return;
            }

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir))
            {
                StatusMessage.Text = "Save directory does not exist.";
                return;
            }

            var picked = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = $"Select Thumbnail (.btl) for {selected.DisplayName}",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("SMM2 Course Thumbnail (.btl)")
                    {
                        Patterns = new[] { "*.btl" }
                    }
                }
            });

            if (picked.Count == 0) return;

            try
            {
                byte[] btlBytes = await File.ReadAllBytesAsync(picked[0].Path.LocalPath);
                SaveManagerService.WriteThumbnail(saveDir, selected.SlotIndex, btlBytes);
                StatusMessage.Text = $"Successfully imported thumbnail '{picked[0].Name}' into {selected.DisplayName}!";
                RefreshSlots();
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Failed to import thumbnail: {ex.Message}";
            }
        }

        private async void OnExportBtl(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null || !selected.HasThumbnail)
            {
                StatusMessage.Text = "Please select a slot with an existing thumbnail to export.";
                return;
            }

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            var targets = SaveManagerService.GetTargetDirectories(saveDir);
            string srcFile = $"course_thumb_{selected.SlotIndex:D3}.btl";
            string? foundPath = null;
            foreach (var t in targets)
            {
                string p = Path.Combine(t, srcFile);
                if (File.Exists(p)) { foundPath = p; break; }
            }

            if (foundPath == null)
            {
                StatusMessage.Text = "Thumbnail file could not be found.";
                return;
            }

            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = $"Export Thumbnail for {selected.DisplayName}",
                DefaultExtension = $"course_thumb_{selected.SlotId}.btl",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("SMM2 Course Thumbnail (.btl)")
                    {
                        Patterns = new[] { "*.btl" }
                    }
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

        private async void OnExportImage(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null || !selected.HasThumbnail)
            {
                StatusMessage.Text = "Please select a slot with an existing thumbnail to export.";
                return;
            }

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            var targets = SaveManagerService.GetTargetDirectories(saveDir);
            string srcFile = $"course_thumb_{selected.SlotIndex:D3}.btl";
            string? foundPath = null;
            foreach (var t in targets)
            {
                string p = Path.Combine(t, srcFile);
                if (File.Exists(p)) { foundPath = p; break; }
            }

            if (foundPath == null)
            {
                StatusMessage.Text = "Thumbnail file could not be found.";
                return;
            }

            try
            {
                byte[] btlBytes = await File.ReadAllBytesAsync(foundPath);
                byte[] jpegBytes = ThumbnailCrypto.DecryptThumbnail(btlBytes);

                if (jpegBytes == null || jpegBytes.Length == 0)
                {
                    StatusMessage.Text = "Thumbnail is empty or uninitialized.";
                    return;
                }

                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = $"Export Thumbnail Image for {selected.DisplayName}",
                    SuggestedFileName = $"course_thumb_{selected.SlotId}.jpg",
                    DefaultExtension = "jpg",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("JPEG Image (*.jpg)")
                        {
                            Patterns = new[] { "*.jpg", "*.jpeg" }
                        },
                        new FilePickerFileType("All Files (*.*)")
                        {
                            Patterns = new[] { "*.*" }
                        }
                    }
                });

                if (file == null) return;

                await File.WriteAllBytesAsync(file.Path.LocalPath, jpegBytes);
                StatusMessage.Text = $"Exported thumbnail image to '{file.Name}'.";
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Failed to export thumbnail image: {ex.Message}";
            }
        }

        private async void OnCloneThumb(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null)
            {
                StatusMessage.Text = "Please select a destination slot first.";
                return;
            }

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir))
            {
                StatusMessage.Text = "Save directory does not exist.";
                return;
            }

            var availableSlots = slots.Where(s => s.HasThumbnail && s.SlotIndex != selected.SlotIndex).ToList();
            if (availableSlots.Count == 0)
            {
                StatusMessage.Text = "No other slots with thumbnails found to clone from.";
                return;
            }

            var dialog = new Window
            {
                Title = $"Clone Thumbnail into {selected.DisplayName}",
                Width = 440,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = (IBrush)this.FindResource("AppBackgroundBrush")!
            };

            var stack = new StackPanel { Margin = new Thickness(16), Spacing = 12 };
            stack.Children.Add(new TextBlock 
            { 
                Text = $"Select a source slot to copy thumbnail into {selected.DisplayName} ({selected.InGameSlot}):", 
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = (IBrush)this.FindResource("TextPrimaryBrush")!
            });

            var combo = new ComboBox 
            { 
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ItemsSource = availableSlots.Select(s => $"{s.DisplayName} ({s.InGameSlot}) - {s.Title}").ToList(),
                SelectedIndex = 0
            };
            stack.Children.Add(combo);

            var btnStack = new StackPanel 
            { 
                Orientation = Avalonia.Layout.Orientation.Horizontal, 
                Spacing = 10, 
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
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
                    else
                    {
                        StatusMessage.Text = "Failed to copy thumbnail.";
                    }
                }
                dialog.Close();
            };

            cancelBtn.Click += (s, ev) => dialog.Close();
            await dialog.ShowDialog(this);
        }

        private void OnDeleteThumb(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedSlot();
            if (selected == null || !selected.HasThumbnail)
            {
                StatusMessage.Text = "Please select a slot with a thumbnail to delete.";
                return;
            }

            string saveDir = SavePathBox.Text?.Trim() ?? "";
            SaveManagerService.DeleteThumbnail(saveDir, selected.SlotIndex);
            StatusMessage.Text = $"Deleted thumbnail from {selected.DisplayName}.";
            RefreshSlots();
        }

        private void OnBackupSave(object? sender, RoutedEventArgs e)
        {
            string saveDir = SavePathBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir))
            {
                StatusMessage.Text = "Save directory does not exist.";
                return;
            }

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

        private void OnClose(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
