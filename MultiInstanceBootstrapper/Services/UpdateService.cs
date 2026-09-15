using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using MultiInstanceBootstrapper.Helpers;

namespace MultiInstanceBootstrapper.Services;

public class UpdateInfo
{
    public string Version { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Changelog { get; set; } = string.Empty;
    public bool HasUpdate { get; set; }
}

public class UpdateService
{
    private readonly HttpClient _httpClient;
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _currentVersion;

    public UpdateService(string currentVersion, string owner = Constants.GitHubOwner, string repo = Constants.GitHubRepo)
    {
        _currentVersion = currentVersion;
        _owner = owner;
        _repo = repo;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MultiInstanceBootstrapper");
    }

    public async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new UpdateInfo { HasUpdate = false };

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<JsonElement>(json);

            var latestVersion = release.GetProperty("tag_name").GetString()?.TrimStart('v') ?? string.Empty;
            var htmlUrl = release.GetProperty("html_url").GetString() ?? string.Empty;
            var changelog = release.GetProperty("body").GetString() ?? string.Empty;

            return new UpdateInfo
            {
                Version = latestVersion,
                Url = htmlUrl,
                Changelog = changelog,
                HasUpdate = IsNewerVersion(latestVersion, _currentVersion)
            };
        }
        catch
        {
            return new UpdateInfo { HasUpdate = false };
        }
    }

    public async Task<string?> DownloadLatestReleaseAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<JsonElement>(json);

            var assets = release.GetProperty("assets");
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? string.Empty;
                if (name.EndsWith(".zip") && name.Contains("MultiInstance"))
                {
                    var browserUrl = asset.GetProperty("browser_download_url").GetString() ?? string.Empty;
                    var zipPath = await DownloadFileAsync(browserUrl);
                    if (zipPath != null)
                    {
                        return ExtractRelease(zipPath);
                    }
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private string? ExtractRelease(string zipPath)
    {
        try
        {
            var extractDir = Path.Combine(Path.GetTempPath(), $"MIB_update_{Guid.NewGuid():N}");
            ZipFile.ExtractToDirectory(zipPath, extractDir);
            File.Delete(zipPath);

            var exe = Directory.GetFiles(extractDir, "*.exe", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Contains("MultiInstance", StringComparison.OrdinalIgnoreCase));

            if (exe == null)
                exe = Directory.GetFiles(extractDir, "*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault();

            return exe;
        }
        catch
        {
            return null;
        }
    }

    public bool ApplyUpdate(string newExePath)
    {
        try
        {
            var currentExe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(currentExe) || !File.Exists(newExePath))
                return false;

            var scriptDir = Path.GetDirectoryName(currentExe) ?? Path.GetTempPath();
            var scriptPath = Path.Combine(Path.GetTempPath(), $"MIB_apply_{Guid.NewGuid():N}.cmd");
            var newExeEscaped = newExePath.Replace("\"", "\\\"");
            var currentExeEscaped = currentExe.Replace("\"", "\\\"");
            var selfPid = Environment.ProcessId;

            var script = $@"@echo off
start /b powershell -NoProfile -WindowStyle Hidden -Command ""Start-Sleep -Seconds 2; while (Get-Process -Id {selfPid} -ErrorAction SilentlyContinue) {{ Start-Sleep -Milliseconds 200 }}; Copy-Item -LiteralPath '{newExeEscaped}' -Destination '{currentExeEscaped}' -Force; Remove-Item -LiteralPath '{newExeEscaped}' -Force -ErrorAction SilentlyContinue; Start-Process -FilePath '{currentExeEscaped}'""
exit /b 0";

            File.WriteAllText(scriptPath, script);

            var psi = new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(currentExe) ?? ""
            };

            Process.Start(psi);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> DownloadFileAsync(string url)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"MIB_{Guid.NewGuid()}.zip");
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
            await stream.CopyToAsync(fileStream);
            return tempPath;
        }
        catch
        {
            return null;
        }
    }

    private bool IsNewerVersion(string latest, string current)
    {
        try
        {
            var latestParts = latest.Split('.').Select(int.Parse).ToArray();
            var currentParts = current.Split('.').Select(int.Parse).ToArray();
            for (int i = 0; i < Math.Max(latestParts.Length, currentParts.Length); i++)
            {
                var l = i < latestParts.Length ? latestParts[i] : 0;
                var c = i < currentParts.Length ? currentParts[i] : 0;
                if (l > c) return true;
                if (l < c) return false;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
