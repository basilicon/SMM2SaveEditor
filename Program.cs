using Avalonia;
using System;
using System.Diagnostics;
using System.Threading;

namespace SMM2SaveEditor;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
#if DEBUG
        Trace.Listeners.Add(new TextWriterTraceListener("./mylog.txt"));
        Trace.AutoFlush = true;
        Trace.Indent();
#endif

        Debug.WriteLine("Starting app...");
        if (args.Length > 0)
        {
            Thread thread1 = new Thread(() => OpenFileImmediately(args[0]));
            thread1.Start();
        }

        BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    public static void OpenFileImmediately(string path)
    {
        while (MainWindow.Instance == null)
        {
            Thread.Sleep(1000);
        }
        MainWindow.Instance.Loaded += delegate
        {
            try
            {
                MainWindow.Instance.LoadFromFile(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        };
    }
}
