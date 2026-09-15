using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace MultiInstanceUpdater;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: MultiInstanceUpdater <targetPath> <zipPath>");
            return;
        }

        var targetPath = args[0];
        var zipPath = args[1];

        try
        {
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
                Thread.Sleep(500);
            }

            if (File.Exists(zipPath))
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, Path.GetDirectoryName(targetPath) ?? ".");
                var extractedFile = Path.Combine(Path.GetDirectoryName(targetPath) ?? ".", Path.GetFileNameWithoutExtension(zipPath) + ".exe");
                var targetFile = Path.GetFileName(targetPath);
                var foundFile = Directory.GetFiles(Path.GetDirectoryName(targetPath) ?? ".", "*.exe", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(f => Path.GetFileName(f) != Path.GetFileName(zipPath));
                if (foundFile != null)
                {
                    File.Move(foundFile, targetPath, true);
                }
                File.Delete(zipPath);
            }

            Process.Start(targetPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update failed: {ex.Message}");
            Process.Start(targetPath);
        }
    }
}
