using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace NodeRadarPro.Core;

public static class UpdateService
{
    public static async Task<(bool hasUpdate, string version, string downloadUrl)> CheckForUpdatesAsync(string currentVersion, HttpClient? customClient = null)
    {
        try
        {
            var http = customClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            string response;
            try
            {
                if (!http.DefaultRequestHeaders.UserAgent.TryParseAdd("NodeRadarPro/1.0"))
                {
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("NodeRadarPro/1.0");
                }
                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/pypiestudio/noderadar-pro/releases/latest");
                var result = await http.SendAsync(request);
                result.EnsureSuccessStatusCode();
                response = await result.Content.ReadAsStringAsync();
            }
            finally
            {
                if (customClient == null) http.Dispose();
            }
            
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
        catch { }
        return (false, "", "");
    }

    public static async Task DownloadAndInstallAsync(string downloadUrl, Action<double>? progressCallback = null)
    {
        string tempFile = Path.Combine(Path.GetTempPath(), "NodeRadarPro_Update.exe");
        string batFile = Path.Combine(Path.GetTempPath(), "noderadar_updater.bat");
        
        using var http = new HttpClient();
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
start /wait """" ""{tempFile}"" /SILENT
echo Restarting NodeRadar Pro...
start """" ""{currentExe}""
del ""%~f0""
";
        File.WriteAllText(batFile, batContent);

        Process.Start(new ProcessStartInfo
        {
            FileName = batFile,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        });

        Environment.Exit(0);
    }
}
