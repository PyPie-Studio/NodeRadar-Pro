using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NodeRadarPro.Core
{
    public static class AppUtils
    {
        public static void OpenSafeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    return;
                }

                string safeUrl = uri.AbsoluteUri;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    psi.ArgumentList.Add("/c");
                    psi.ArgumentList.Add("start");
                    psi.ArgumentList.Add("");
                    psi.ArgumentList.Add(safeUrl);
                    Process.Start(psi);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        UseShellExecute = false
                    };
                    psi.ArgumentList.Add(safeUrl);
                    Process.Start(psi);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "open",
                        UseShellExecute = false
                    };
                    psi.ArgumentList.Add(safeUrl);
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "AppUtils", $"Failed to open safe URL {url}: {ex.Message}");
            }
        }
    }
}
