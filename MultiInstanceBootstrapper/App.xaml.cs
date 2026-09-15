using System;
using System.IO;
using System.Windows;

namespace MultiInstanceBootstrapper;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
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
