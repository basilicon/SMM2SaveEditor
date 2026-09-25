using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
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
        public Level CurrentLevel => level;

        private EntityEditor entityEditor;
        private ZoomBorder? zoomBorder;

        private IStorageBookmarkFile? storageBookmarkFile;
        private string? currentFilePath;

        private SaveSlotManagerControl? saveSlotManager;
        private Grid? editingArea;
        private Button? tabSlotsBtn;
        private Button? tabEditorBtn;
        private StackPanel? editorControlsBar;
        private StackPanel? slotsControlsBar;
        private Border? zoomBadge;

        private TextBlock? courseHeaderTitle;
        private Border? courseHeaderStyleBadge;
        private TextBlock? courseHeaderStyleText;
        private Border? statusDot;
        private TextBlock? statusText;
        private TextBlock? coordsText;
        private TextBlock? zoomText;

        public MainWindow()
        {
            Instance = this;

            this.InitializeComponent();

            level = this.Find<Level>("Level")!;

            saveSlotManager = this.Find<SaveSlotManagerControl>("SaveSlotManager");
            editingArea = this.Find<Grid>("EditingArea");
            tabSlotsBtn = this.Find<Button>("TabSlotsBtn");
            tabEditorBtn = this.Find<Button>("TabEditorBtn");
            editorControlsBar = this.Find<StackPanel>("EditorControlsBar");
            slotsControlsBar = this.Find<StackPanel>("SlotsControlsBar");
            zoomBadge = this.Find<Border>("ZoomBadge");

            courseHeaderTitle = this.Find<TextBlock>("CourseHeaderTitle");
            courseHeaderStyleBadge = this.Find<Border>("CourseHeaderStyleBadge");
            courseHeaderStyleText = this.Find<TextBlock>("CourseHeaderStyleText");
            statusDot = this.Find<Border>("StatusDot");
            statusText = this.Find<TextBlock>("StatusText");
            coordsText = this.Find<TextBlock>("CoordsText");
            zoomText = this.Find<TextBlock>("ZoomText");

            if (saveSlotManager != null)
            {
                saveSlotManager.RequestOpenLevel += path =>
                {
                    LoadFromFile(path);
                    SwitchToTab(showSlots: false);
                };
                saveSlotManager.RequestCurrentLevel += () => level;
            }

            entityEditor = new();
            editingArea?.Children.Add(entityEditor);
            Grid.SetColumn(entityEditor, 2);

            zoomBorder = this.Find<ZoomBorder>("ZoomBorder");
            if (zoomBorder == null) throw new MissingMemberException("No zoom border found!");
            zoomBorder.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Space) { zoomBorder.UniformToFill(); UpdateZoomText(); }
                if (e.Key == Avalonia.Input.Key.R) { zoomBorder.ResetMatrix(); UpdateZoomText(); }
                if (e.Key == Avalonia.Input.Key.OemPlus) { zoomBorder.ZoomIn(); UpdateZoomText(); }
                if (e.Key == Avalonia.Input.Key.OemMinus) { zoomBorder.ZoomOut(); UpdateZoomText(); }
            };
            zoomBorder.PointerWheelChanged += (s, e) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(UpdateZoomText);
            };
            zoomBorder.PointerMoved += (s, e) =>
            {
                if (level != null && coordsText != null)
                {
                    var pos = e.GetPosition(level);
                    coordsText.Text = $"X: {(int)pos.X}  Y: {(int)pos.Y}";
                }
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

            // Default to Coursebot Save Slots View on Launch!
            SwitchToTab(showSlots: true);

            Debug.WriteLine("Launched application!");
        }

        public void SwitchToTab(bool showSlots)
        {
            if (saveSlotManager != null) saveSlotManager.IsVisible = showSlots;
            if (editingArea != null) editingArea.IsVisible = !showSlots;

            if (tabSlotsBtn != null)
            {
                if (showSlots) tabSlotsBtn.Classes.Add("active");
                else tabSlotsBtn.Classes.Remove("active");
            }

            if (tabEditorBtn != null)
            {
                if (!showSlots) tabEditorBtn.Classes.Add("active");
                else tabEditorBtn.Classes.Remove("active");
            }

            if (editorControlsBar != null) editorControlsBar.IsVisible = !showSlots;
            if (slotsControlsBar != null) slotsControlsBar.IsVisible = showSlots;
            if (coordsText != null) coordsText.IsVisible = !showSlots;
            if (zoomBadge != null) zoomBadge.IsVisible = !showSlots;

            if (showSlots)
            {
                if (courseHeaderTitle != null) courseHeaderTitle.Text = "Coursebot Save Slots Manager";
                if (courseHeaderStyleBadge != null) courseHeaderStyleBadge.IsVisible = false;
                if (statusText != null) statusText.Text = "Coursebot Save Slot Manager active";
                Title = "SMM2 Course & Save Editor - Coursebot Slots";
            }
            else
            {
                string displayName = level != null && !string.IsNullOrWhiteSpace(level.levelName)
                    ? level.levelName 
                    : (!string.IsNullOrEmpty(currentFilePath) ? Path.GetFileName(currentFilePath) : "Level Editor");

                if (courseHeaderTitle != null) courseHeaderTitle.Text = displayName;
                if (courseHeaderStyleBadge != null && level != null)
                {
                    courseHeaderStyleBadge.IsVisible = true;
                    if (courseHeaderStyleText != null) courseHeaderStyleText.Text = level.gameStyle.ToString();
                }
                Title = $"SMM2 Course Editor - {displayName}";
                UpdateZoomText();
            }
        }

        private void OnSelectSlotsTab(object? sender, RoutedEventArgs e)
        {
            SwitchToTab(showSlots: true);
        }

        private void OnSelectEditorTab(object? sender, RoutedEventArgs e)
        {
            SwitchToTab(showSlots: false);
        }

        private void UpdateZoomText()
        {
            if (zoomBorder != null && zoomText != null)
            {
                int pct = (int)Math.Round(zoomBorder.ZoomX * 100);
                zoomText.Text = $"Zoom: {pct}%";
            }
        }

        private void OnZoomIn(object? sender, RoutedEventArgs e)
        {
            zoomBorder?.ZoomIn();
            UpdateZoomText();
        }

        private void OnZoomOut(object? sender, RoutedEventArgs e)
        {
            zoomBorder?.ZoomOut();
            UpdateZoomText();
        }

        private void OnZoomFit(object? sender, RoutedEventArgs e)
        {
            zoomBorder?.UniformToFill();
            UpdateZoomText();
        }

        private void OnResetZoom(object? sender, RoutedEventArgs e)
        {
            zoomBorder?.ResetMatrix();
            UpdateZoomText();
        }

        private void OnExitApp(object? sender, RoutedEventArgs e)
        {
            Close();
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
            if (statusDot != null && this.TryFindResource("SmmGreenBrush", out var greenBrush)) statusDot.Background = (IBrush)greenBrush!;
            if (statusText != null) statusText.Text = $"Exported to {Path.GetFileName(picked.Path.LocalPath)}";
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
            if (statusDot != null && this.TryFindResource("SmmGreenBrush", out var greenBrush)) statusDot.Background = (IBrush)greenBrush!;
            if (statusText != null) statusText.Text = $"Saved to {Path.GetFileName(savePath)}";
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

                string displayName = !string.IsNullOrWhiteSpace(level.levelName) ? level.levelName : Path.GetFileName(path);
                Title = $"SMM2 Course Editor - {displayName}";
                if (courseHeaderTitle != null) courseHeaderTitle.Text = displayName;
                if (courseHeaderStyleBadge != null) courseHeaderStyleBadge.IsVisible = true;
                if (courseHeaderStyleText != null) courseHeaderStyleText.Text = level.gameStyle.ToString();
                if (statusDot != null && this.TryFindResource("SmmGreenBrush", out var greenBrush)) statusDot.Background = (IBrush)greenBrush!;
                if (statusText != null) statusText.Text = $"Loaded: {Path.GetFileName(path)} ({(bytes.Length / 1024)} KB)";
                UpdateZoomText();
                SwitchToTab(showSlots: false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                if (statusDot != null && this.TryFindResource("SmmRedBrush", out var redBrush)) statusDot.Background = (IBrush)redBrush!;
                if (statusText != null) statusText.Text = $"Error: {ex.Message}";
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

        private void OnOpenSaveSlotManager(object sender, RoutedEventArgs e)
        {
            SwitchToTab(showSlots: true);
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