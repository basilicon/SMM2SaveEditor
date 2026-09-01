using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Markup.Xaml;
using SMM2SaveEditor;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using Avalonia.Input;
using Kaitai;
using SMM2SaveEditor.Utility;
using System.IO;
using System.Diagnostics;
using SMM2SaveEditor.Utility.EditorHelpers;
using System;
using Avalonia.Media.Imaging;

namespace SMM2SaveEditor
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }

        private Level level;
        private EntityEditor entityEditor;
        private ZoomBorder? zoomBorder;

        private IStorageBookmarkFile? storageBookmarkFile;
        private string? currentFilePath;

        public MainWindow()
        {
            Instance = this;

            this.InitializeComponent();

            level = this.Find<Level>("Level");

            entityEditor = new();
            this.Find<Grid>("EditingArea")?.Children.Add(entityEditor);
            Grid.SetColumn(entityEditor, 2);

            zoomBorder = this.Find<ZoomBorder>("ZoomBorder");
            if (zoomBorder == null) throw new MissingMemberException("No zoom border found!");
            zoomBorder.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Space) zoomBorder.UniformToFill();
            };

            var iconPath = AssetHelper.GetAssetFilePath("Assets/smm2saveeditor.ico");
            if (iconPath != null)
            {
                try
                {
                    Icon = new WindowIcon(iconPath);
                }
                catch { }
            }

            DragDrop.SetAllowDrop(this, true);
            AddHandler(DragDrop.DropEvent, OnDrop);

            Debug.WriteLine("Launched application!");
        }

        private void InitializeComponent()
        {
            Debug.WriteLine("Initializing window...");
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDrop(object? sender, DragEventArgs e)
        {
            var files = e.Data.GetFiles();
            if (files != null)
            {
                foreach (var file in files)
                {
                    string path = file.Path.LocalPath;
                    if (path.EndsWith(".bcd", StringComparison.OrdinalIgnoreCase))
                    {
                        LoadFromFile(path);
                        break;
                    }
                }
            }
        }

        private async void OnOpenLevel(object sender, RoutedEventArgs e)
        {
            var picked = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Open Level",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("")
                    {
                        Patterns = new[] { "*.bcd" }
                    }
                }
            });

            if (picked.Count == 0) return;

            storageBookmarkFile = (IStorageBookmarkFile)picked[0];
            LoadFromFile(storageBookmarkFile.Path.LocalPath);
        }

        private async void OnExportLevel(object sender, RoutedEventArgs e)
        {
            string defaultName = storageBookmarkFile != null 
                ? storageBookmarkFile.Name 
                : (!string.IsNullOrEmpty(currentFilePath) ? Path.GetFileName(currentFilePath) : "course_data_000.bcd");

            IStorageFile? picked = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "Export Level",
                DefaultExtension = defaultName,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("") 
                    {
                        Patterns = new[] { "*.bcd" }
                    }
                },
                ShowOverwritePrompt = true
            });

            if (picked == null) return;

            byte[] encrypted = LevelCrypto.EncryptLevel(level.GetBytes());
            File.WriteAllBytes(picked.Path.LocalPath, encrypted);
            currentFilePath = picked.Path.LocalPath;
            Debug.WriteLine("Completed export. Happy trolling!");
        }

        private async void OnSaveLevel(object sender, RoutedEventArgs e)
        {
            string? savePath = storageBookmarkFile?.Path.LocalPath ?? currentFilePath;
            if (string.IsNullOrEmpty(savePath))
            {
                OnExportLevel(sender, e);
                return;
            }

            byte[] encrypted = LevelCrypto.EncryptLevel(level.GetBytes());
            await File.WriteAllBytesAsync(savePath, encrypted);
            Debug.WriteLine($"Saved level to {savePath}");
        }

        public async void LoadFromFile(string path)
        {
            Debug.WriteLine("Attempting to load level from " + path);
            currentFilePath = path;

            try
            {
                byte[] bytes = await File.ReadAllBytesAsync(path);
                bytes = LevelCrypto.DecryptLevel(bytes);
                level.LoadFromStream(new KaitaiStream(bytes));
                Title = $"SMM2SaveEditor - {Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void OnRegisterAssociation(object sender, RoutedEventArgs e)
        {
            try
            {
                RegisterBcdAssociation();
                Debug.WriteLine("Registered .bcd file association in Windows.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to register file association: {ex.Message}");
            }
        }

        public static void RegisterBcdAssociation()
        {
            if (!OperatingSystem.IsWindows()) return;

            string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath ?? "";
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) return;

            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\.bcd"))
            {
                key?.SetValue("", "SMM2SaveEditor.bcd");
            }

            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\SMM2SaveEditor.bcd"))
            {
                key?.SetValue("", "Super Mario Maker 2 Course File");
                using var iconKey = key?.CreateSubKey("DefaultIcon");
                iconKey?.SetValue("", $"\"{exePath}\",0");
                using var cmdKey = key?.CreateSubKey(@"shell\open\command");
                cmdKey?.SetValue("", $"\"{exePath}\" \"%1\"");
            }

            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\Applications\SMM2SaveEditor.exe"))
            {
                using var suppKey = key?.CreateSubKey("SupportedTypes");
                suppKey?.SetValue(".bcd", "");
                using var cmdKey = key?.CreateSubKey(@"shell\open\command");
                cmdKey?.SetValue("", $"\"{exePath}\" \"%1\"");
            }
        }
    }
}