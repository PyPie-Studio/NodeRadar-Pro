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
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = safeUrl,
                        UseShellExecute = true
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = safeUrl,
                        UseShellExecute = false
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = safeUrl,
                        UseShellExecute = false
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "AppUtils", $"Failed to open safe URL {url}: {ex.Message}");
            }
        }
    }
}
