using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Markup.Xaml;
using SMM2Level;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;

namespace SMM2SaveEditor
{
    public partial class MainWindow : Window
    {
        private readonly ZoomBorder? _zoomBorder;
        private Level level;
        private Canvas? canvas;

        public MainWindow()
        {
            this.InitializeComponent();

            _zoomBorder = this.Find<ZoomBorder>("ZoomBorder");
            canvas = this.Find<Canvas>("LevelCanvas");

            level = new();
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

            var file = (IStorageBookmarkFile)picked[0];
            level.LoadFromFile(file.Path.LocalPath);
        }

    }
}