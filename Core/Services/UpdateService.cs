using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security;

namespace NodeRadarPro.Core;

public static class UpdateService
{
    public static async Task<(bool hasUpdate, string version, string downloadUrl)> CheckForUpdatesAsync(string currentVersion, HttpClient? customClient = null)
    {
        try
        {
            var http = customClient;
            bool disposeClient = false;
            if (http == null)
            {
                http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                disposeClient = true;
            }

            try
            {
                if (!http.DefaultRequestHeaders.Contains("User-Agent"))
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("NodeRadarPro/1.0");

                var response = await http.GetStringAsync("https://api.github.com/repos/PyPie-Studio/NodeRadar-Pro/releases/latest");

                var tagIdx = response.IndexOf("\"tag_name\"");
                if (tagIdx < 0) return (false, "", "");

                var valStart = response.IndexOf('"', tagIdx + 11) + 1;
                var valEnd = response.IndexOf('"', valStart);
                var latestVersion = response[valStart..valEnd].TrimStart('v');

                if (latestVersion != currentVersion && !string.IsNullOrEmpty(latestVersion))
                {
                    var urlStartPattern = "\"browser_download_url\": \"";
                    var urlStartIdx = response.IndexOf(urlStartPattern);
                    while (urlStartIdx > 0)
                    {
                        var urlStart = urlStartIdx + urlStartPattern.Length;
                        var urlEnd = response.IndexOf('"', urlStart);
                        var downloadUrl = response[urlStart..urlEnd];
                        if (downloadUrl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            return (true, latestVersion, downloadUrl);
                        }
                        urlStartIdx = response.IndexOf(urlStartPattern, urlEnd);
                    }
                }
            }
            finally
            {
                if (disposeClient) http.Dispose();
            }
        }
        catch { }
        return (false, "", "");
    }

    public static async Task DownloadAndInstallAsync(string downloadUrl, Action<double>? progressCallback = null, HttpClient? customClient = null)
    {
        if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
            !uri.AbsolutePath.StartsWith("/pypiestudio/noderadar-pro/releases/download/", StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityException("Invalid or untrusted download URL.");
        }

        string tempFile = Path.Combine(Path.GetTempPath(), "NodeRadarPro_Update.exe");
        string batFile = Path.Combine(Path.GetTempPath(), "noderadar_updater.bat");

        var http = customClient;
        bool disposeClient = false;
        if (http == null)
        {
            http = new HttpClient();
            disposeClient = true;
        }

        try
        {
            if (!http.DefaultRequestHeaders.Contains("User-Agent"))
                http.DefaultRequestHeaders.UserAgent.ParseAdd("NodeRadarPro/1.0");

            using var response = await http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1 && progressCallback != null;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            var isMoreToRead = true;
            var totalRead = 0L;

            do
            {
                var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    isMoreToRead = false;
                }
                else
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    totalRead += read;
                    if (canReportProgress)
                    {
                        progressCallback!((double)totalRead / totalBytes * 100);
                    }
                }
            }
            while (isMoreToRead);

            string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";

            string batContent = $@"@echo off
echo Waiting for NodeRadar Pro to close...
timeout /t 2 /nobreak >nul
echo Installing update...
start /wait """" ""%UPDATE_EXE%"" /SILENT
echo Restarting NodeRadar Pro...
start """" ""%CURRENT_EXE%""
del ""%~f0""
";
            await File.WriteAllTextAsync(batFile, batContent);

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add(batFile);
            startInfo.Environment["UPDATE_EXE"] = tempFile;
            startInfo.Environment["CURRENT_EXE"] = currentExe;

            Process.Start(startInfo);

            Environment.Exit(0);
        }
        finally
        {
            if (disposeClient) http.Dispose();
        }
    }
}
