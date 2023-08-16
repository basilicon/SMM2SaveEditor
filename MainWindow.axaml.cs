using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Markup.Xaml;
using SMM2Level;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using Kaitai;
using SMM2Level.Utility;
using System.IO;
using System.Diagnostics;
using Avalonia.Input;
using System;

namespace SMM2SaveEditor
{
    public partial class MainWindow : Window
    {
        private readonly ZoomBorder? _zoomBorder;
        private readonly ScrollViewer? _scrollViewer;
        private Level level;
        private Grid? levelGrid;

        public MainWindow()
        {
            this.InitializeComponent();

            _zoomBorder = this.Find<ZoomBorder>("ZoomBorder");

            levelGrid = this.Find<Grid>("LevelGrid");
            level = new();

            Debug.WriteLine("Launched application!");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ZoomBorder_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            Point pointerPosition = e.GetPosition(_zoomBorder);
            IInputElement? clickedElement = _zoomBorder.InputHitTest(pointerPosition);

            if (clickedElement == null) return;
            if (clickedElement is IEntity entity)
            {
                EntityEditor entityEditor = new();
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