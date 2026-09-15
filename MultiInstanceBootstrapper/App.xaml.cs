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

        try
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            try
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "MIB_error.log"),
                    $"{DateTime.Now}: {ex}\n\nInner: {ex.InnerException}");
            }
            catch { }
            throw;
        }
    }
}
