using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace SMM2SaveEditor;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;

            if (desktop.Args != null && desktop.Args.Length > 0)
            {
                string firstArg = desktop.Args[0];
                if (firstArg.Equals("--register-association", StringComparison.OrdinalIgnoreCase))
                {
                    MainWindow.RegisterBcdAssociation();
                }
                else if (File.Exists(firstArg))
                {
                    mainWindow.Opened += (s, e) =>
                    {
                        mainWindow.LoadFromFile(firstArg);
                    };
                }
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}