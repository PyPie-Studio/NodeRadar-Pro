using System.Collections.Generic;
using System.Text;

namespace NodeRadarPro.Core.Services;

/// <summary>
/// Provides decoupled RFC 4180 compliant CSV export serialization for system logs and network devices.
/// Enforces mandatory 12-hour AM/PM timestamp formatting.
/// </summary>
public static class CsvExportService
{
    public static string ExportLogsToCsv(IEnumerable<LogEntry>? logs)
    {
        if (logs == null)
            return "Timestamp,Level,Source,Message,Device\r\n";

        var logList = logs as IList<LogEntry> ?? new List<LogEntry>(logs);
        var sb = new StringBuilder(logList.Count * 128 + 64);
        sb.AppendLine("Timestamp,Level,Source,Message,Device");

        foreach (var log in logList)
        {
            if (log == null) continue;

            string levelStr = log.Level switch
            {
                LogLevel.Info => "Info",
                LogLevel.Warning => "Warning",
                LogLevel.Error => "Error",
                _ => log.Level.ToString()
            };

            sb.Append(AppUtils.EscapeCsvCell(log.Timestamp.ToString("yyyy-MM-dd  hh:mm:ss tt")))
              .Append(',')
              .Append(AppUtils.EscapeCsvCell(levelStr))
              .Append(',')
              .Append(AppUtils.EscapeCsvCell(log.Source))
              .Append(',')
              .Append(AppUtils.EscapeCsvCell(log.Message))
              .Append(',')
              .AppendLine(AppUtils.EscapeCsvCell(log.DeviceMac));
        }

        return sb.ToString();
    }

    public static string ExportNodesToCsv(IEnumerable<NetworkNode>? nodes)
    {
        if (nodes == null)
            return "IP Address,MAC Address,Hostname,Vendor,Device Type,Status,Latency (ms),Open Ports,OS Guess\r\n";

        var nodeList = nodes as IList<NetworkNode> ?? new List<NetworkNode>(nodes);
        var sb = new StringBuilder(nodeList.Count * 128 + 128);
        sb.AppendLine("IP Address,MAC Address,Hostname,Vendor,Device Type,Status,Latency (ms),Open Ports,OS Guess");

        foreach (var node in nodeList)
        {
            if (node == null) continue;

            string ports = node.OpenPorts?.Count > 0 ? string.Join(";", node.OpenPorts) : "";
            string status = node.IsOnline ? "Online" : "Offline";

            sb.Append(AppUtils.EscapeCsvCell(node.IpAddress))
              .Append(',')
              .Append(AppUtils.EscapeCsvCell(node.MacAddress))
              .Append(',')
              .Append(AppUtils.EscapeCsvCell(node.Hostname))
              .Append(',')
              .Append(AppUtils.EscapeCsvCell(node.Vendor))
              .Append(',')
              .Append(AppUtils.EscapeCsvCell(node.DeviceType))
              .Append(',')
              .Append(AppUtils.EscapeCsvCell(status))
              .Append(',')
              .Append(AppUtils.EscapeCsvCell(node.PingLatencyMs.ToString()))
              .Append(',')
              .Append(AppUtils.EscapeCsvCell(ports))
              .Append(',')
              .AppendLine(AppUtils.EscapeCsvCell(node.OsGuess));
        }

        return sb.ToString();
    }
}
