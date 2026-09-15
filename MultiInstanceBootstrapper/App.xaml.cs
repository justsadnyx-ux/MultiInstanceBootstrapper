using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace MultiInstanceBootstrapper;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Force software rendering. Hardware (DirectX) acceleration can fail on machines
        // with broken/remote/virtual GPU drivers, leaving the WPF client area blank white.
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

        DispatcherUnhandledException += (s, e) =>
        {
            LogStartupError(e.Exception, "DispatcherUnhandledException");
            e.Handled = true;
        };

        try
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            LogStartupError(ex, "OnStartup");
            throw;
        }
    }

    public static void LogStartupError(Exception ex, string stage)
    {
        try
        {
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "MIB_error.log"),
                $"{DateTime.Now} [{stage}]: {ex}\n\nInner: {ex.InnerException}\n\n---\n");
        }
        catch { }
    }
}
