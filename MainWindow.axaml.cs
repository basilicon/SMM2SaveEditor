using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Markup.Xaml;
using SMM2SaveEditor;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using Kaitai;
using SMM2SaveEditor.Utility;
using System.IO;
using System.Diagnostics;
using SMM2SaveEditor.Utility.EditorHelpers;
using System;

namespace SMM2SaveEditor
{
    public partial class MainWindow : Window
    {
        private Level level;
        private EntityEditor entityEditor;
        private ZoomBorder? zoomBorder;

        private IStorageBookmarkFile? storageBookmarkFile;

        public MainWindow()
        {
            this.InitializeComponent();

            level = this.Find<Level>("Level");
            level.LoadFromStream(new KaitaiStream(new byte[0x5BFD0]));

            entityEditor = new();
            this.Find<Grid>("EditingArea")?.Children.Add(entityEditor);
            Grid.SetColumn(entityEditor, 2);

            zoomBorder = this.Find<ZoomBorder>("ZoomBorder");
            if (zoomBorder == null) throw new MissingMemberException("No zoom border found!");
            zoomBorder.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Space) zoomBorder.UniformToFill();
            };

            Debug.WriteLine("Launched application!");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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
            IStorageFile? picked = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "Export Level",
                DefaultExtension = (storageBookmarkFile != null) ? storageBookmarkFile.Name : "course_data_000.bcd",
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
            Debug.WriteLine("Completed export. Happy trolling!");
        }

        private async void OnSaveLevel(object sender, RoutedEventArgs e)
        {
            if (storageBookmarkFile == null)
            {
                OnExportLevel(sender, e);
                return;
            }

            await File.WriteAllBytesAsync(storageBookmarkFile.Path.LocalPath, level.GetBytes());
        }

        private async void LoadFromFile(string path)
        {
            byte[] bytes = await File.ReadAllBytesAsync(path);

            bytes = LevelCrypto.DecryptLevel(bytes);

            level.LoadFromStream(new KaitaiStream(bytes));
        }
    }
}