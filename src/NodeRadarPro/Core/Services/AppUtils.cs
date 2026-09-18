using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace NodeRadarPro.Core
{
    public class NetworkInterfaceInfo
    {
        public string Name { get; set; } = string.Empty;
        public OperationalStatus OperationalStatus { get; set; } = OperationalStatus.Unknown;
        public NetworkInterfaceType NetworkInterfaceType { get; set; } = NetworkInterfaceType.Unknown;
        public byte[] PhysicalAddressBytes { get; set; } = Array.Empty<byte>();
        public List<IPAddress> UnicastAddresses { get; set; } = new();
    }

    public static class AppUtils
    {
        public static string GetMacAddress(Func<IEnumerable<NetworkInterfaceInfo>>? getInterfaces = null)
        {
            try
            {
                var interfaces = getInterfaces != null ? getInterfaces() : GetSystemNetworkInterfaces();
                foreach (var ni in interfaces)
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    var bytes = ni.PhysicalAddressBytes;
                    if (bytes != null && bytes.Length > 0 && bytes.Any(b => b != 0))
                    {
                        return string.Join(":", bytes.Select(b => b.ToString("X2")));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "AppUtils", $"Failed to retrieve MAC address: {ex.Message}");
            }

            return "Unknown";
        }

        public static string GetLocalIpAddress(Func<IEnumerable<NetworkInterfaceInfo>>? getInterfaces = null)
        {
            try
            {
                var interfaces = getInterfaces != null ? getInterfaces() : GetSystemNetworkInterfaces();
                foreach (var ni in interfaces)
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    var ip = ni.UnicastAddresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    if (ip != null)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "AppUtils", $"Failed to retrieve local IP address: {ex.Message}");
            }

            return "127.0.0.1";
        }

        private static IEnumerable<NetworkInterfaceInfo> GetSystemNetworkInterfaces()
        {
            var list = new List<NetworkInterfaceInfo>();
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    var unicast = new List<IPAddress>();
                    try
                    {
                        var ipProps = ni.GetIPProperties();
                        if (ipProps?.UnicastAddresses != null)
                        {
                            foreach (var u in ipProps.UnicastAddresses)
                            {
                                if (u?.Address != null) unicast.Add(u.Address);
                            }
                        }
                    }
                    catch { }

                    byte[] macBytes = Array.Empty<byte>();
                    try
                    {
                        macBytes = ni.GetPhysicalAddress()?.GetAddressBytes() ?? Array.Empty<byte>();
                    }
                    catch { }

                    list.Add(new NetworkInterfaceInfo
                    {
                        Name = ni.Name,
                        OperationalStatus = ni.OperationalStatus,
                        NetworkInterfaceType = ni.NetworkInterfaceType,
                        PhysicalAddressBytes = macBytes,
                        UnicastAddresses = unicast
                    });
                }
            }
            catch { }

            return list;
        }

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
                        FileName = safeUrl,
                        UseShellExecute = true
                    };
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

        public static void OpenFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !System.IO.Directory.Exists(folderPath)) return;

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{folderPath}\"",
                        UseShellExecute = false
                    };
                    Process.Start(psi);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        UseShellExecute = false
                    };
                    psi.ArgumentList.Add(folderPath);
                    Process.Start(psi);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "open",
                        UseShellExecute = false
                    };
                    psi.ArgumentList.Add(folderPath);
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "AppUtils", $"Failed to open folder {folderPath}: {ex.Message}");
            }
        }
    }
}
