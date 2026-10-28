using System;
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

            sb.Append('"')
              .Append(log.Timestamp.ToString("yyyy-MM-dd  hh:mm:ss tt"))
              .Append("\",\"")
              .Append(levelStr)
              .Append("\",\"")
              .Append(EscapeCsv(log.Source))
              .Append("\",\"")
              .Append(EscapeCsv(log.Message))
              .Append("\",\"")
              .Append(EscapeCsv(log.DeviceMac))
              .AppendLine("\"");
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

            sb.Append('"')
              .Append(EscapeCsv(node.IpAddress))
              .Append("\",\"")
              .Append(EscapeCsv(node.MacAddress))
              .Append("\",\"")
              .Append(EscapeCsv(node.Hostname))
              .Append("\",\"")
              .Append(EscapeCsv(node.Vendor))
              .Append("\",\"")
              .Append(EscapeCsv(node.DeviceType))
              .Append("\",\"")
              .Append(status)
              .Append("\",\"")
              .Append(node.PingLatencyMs)
              .Append("\",\"")
              .Append(EscapeCsv(ports))
              .Append("\",\"")
              .Append(EscapeCsv(node.OsGuess))
              .AppendLine("\"");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains('"', StringComparison.Ordinal))
            return value.Replace("\"", "\"\"", StringComparison.Ordinal);
        return value;
    }
}
