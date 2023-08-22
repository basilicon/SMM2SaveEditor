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
using Avalonia.Input;
using System;

namespace SMM2SaveEditor
{
    public partial class MainWindow : Window
    {
        private Level level;
        private Grid? levelGrid;
        private EntityEditor? entityEditor;

        public MainWindow()
        {
            this.InitializeComponent();

            levelGrid = this.Find<Grid>("LevelGrid");
            level = new();

            entityEditor = new();
            this.Find<Canvas>("EditingArea")?.Children.Add(entityEditor);

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

            Debug.WriteLine("OnOpenLevel : Opening a new level!");

            var file = (IStorageBookmarkFile)picked[0];
            LoadFromFile(file.Path.LocalPath);
        }

        private void LoadFromFile(string path)
        {
            // Debug.Log("Decrypting bytes from file...");

            byte[] bytes = File.ReadAllBytes(path);

            bytes = LevelCrypto.DecryptLevel(bytes);
            level.LoadFromStream(new KaitaiStream(bytes), levelGrid);
        }
    }
}