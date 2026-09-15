using System;
using System.Diagnostics;
using System.IO;
using MultiInstanceBootstrapper.Helpers;
using MultiInstanceBootstrapper.Models;
using Microsoft.Win32;

namespace MultiInstanceBootstrapper.Services;

public class RobloxService
{
    private readonly RegistryKey? _registryKey;

    public RobloxService()
    {
        try
        {
            _registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Roblox\RobloxStudio\Bootstrapper");
        }
        catch
        {
            _registryKey = null;
        }
    }

    public string? GetRobloxPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Roblox\RobloxStudio\Bootstrapper");
            var path = key?.GetValue("InstallPath")?.ToString();
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                var exePath = Path.Combine(path, Constants.RobloxExeName);
                if (File.Exists(exePath))
                    return exePath;
            }
        }
        catch { }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Roblox");
            var path = key?.GetValue("InstallPath")?.ToString();
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                var exePath = Path.Combine(path, Constants.RobloxExeName);
                if (File.Exists(exePath))
                    return exePath;
            }
        }
        catch { }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var robloxDir = Path.Combine(localAppData, "Roblox", "Versions");
        if (Directory.Exists(robloxDir))
        {
            foreach (var dir in Directory.GetDirectories(robloxDir))
            {
                var exePath = Path.Combine(dir, Constants.RobloxExeName);
                if (File.Exists(exePath))
                    return exePath;
            }
        }

        return null;
    }

    public Process? LaunchInstance(string robloxPath, int instanceId)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = robloxPath,
                Arguments = $"-instance {instanceId} -id {instanceId}",
                UseShellExecute = false,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal
            };

            var process = Process.Start(startInfo);
            process?.WaitForInputIdle(5000);
            return process;
        }
        catch
        {
            return null;
        }
    }

    public bool KillInstance(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsRobloxInstalled()
    {
        return !string.IsNullOrEmpty(GetRobloxPath());
    }
}
