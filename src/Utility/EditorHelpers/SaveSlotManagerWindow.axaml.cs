using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

                slots = SaveManagerService.ScanSlots(path, 60);
                SlotsListBox.ItemsSource = slots;

                int occupied = slots.Count(s => s.Exists);
                StatusMessage.Text = $"Found {occupied} course slot(s).";
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
                byte[] bcdBytes = await File.ReadAllBytesAsync(picked[0].Path.LocalPath);

                // If file is unencrypted, encrypt it first
                if (bcdBytes.Length == 0x5BFD0 - 0x10 || bcdBytes.Length == 0x5BFD0)
                {
                    bcdBytes = LevelCrypto.EncryptLevel(bcdBytes);
                }

                SaveManagerService.WriteSlot(saveDir, selected.SlotIndex, bcdBytes);
                StatusMessage.Text = $"Successfully imported '{picked[0].Name}' into {selected.DisplayName}!";
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
