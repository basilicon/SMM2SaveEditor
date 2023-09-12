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

        public MainWindow()
        {
            Instance = this;

            this.InitializeComponent();

            level = this.Find<Level>("Level");
            // lol
            //level.LoadFromStream(new KaitaiStream(new byte[0x5BFD0]));

            entityEditor = new();
            this.Find<Grid>("EditingArea")?.Children.Add(entityEditor);
            Grid.SetColumn(entityEditor, 2);

            zoomBorder = this.Find<ZoomBorder>("ZoomBorder");
            if (zoomBorder == null) throw new MissingMemberException("No zoom border found!");
            zoomBorder.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Space) zoomBorder.UniformToFill();
            };

            Icon = new(new Bitmap(
#if RELEASE
                "./Assets/smm2saveeditor.ico"
#else
                "../../../Assets/smm2saveeditor.ico"
#endif
                ));

            

            Debug.WriteLine("Launched application!");
        }

        private void InitializeComponent()
        {
            Debug.WriteLine("Initializing window...");
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

        public async void LoadFromFile(string path)
        {
            Debug.WriteLine("Attempting to load level from " + path);

            try
            {
                byte[] bytes = await File.ReadAllBytesAsync(path);
                bytes = LevelCrypto.DecryptLevel(bytes);
                level.LoadFromStream(new KaitaiStream(bytes));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}