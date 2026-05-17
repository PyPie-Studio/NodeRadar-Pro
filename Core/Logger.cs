using System;
using System.IO;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;

namespace NodeRadarPro.Core;

/// <summary>
/// Professional rolling file logger for technical diagnostics.
/// Writes structured logs to %Documents%/PyPie Studio/NodeRadar Pro/Logs/
/// </summary>
public static class Logger
{
    private static readonly string LogDir;
    private static readonly string CurrentLogFile;
    private static readonly object _fileLock = new();
    private static readonly ConcurrentQueue<string> _logQueue = new();
    private static bool _isProcessing = false;

    static Logger()
    {
        LogDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PyPie Studio", "NodeRadar Pro", "Logs");
        if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);
        
        CurrentLogFile = Path.Combine(LogDir, "noderadar_system.log");
        
        // Initial cleanup of old logs (keep last 5 days)
        CleanupOldLogs();
    }

    public static void Log(LogLevel level, string source, string message, string? deviceMac = null)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string threadId = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString().PadLeft(3, '0');
        string macInfo = !string.IsNullOrEmpty(deviceMac) ? $" [{deviceMac}]" : "";
        
        // Structure: [Timestamp] [Level] [Thread] [Source] Message [Device]
        string line = $"[{timestamp}] [{level.ToString().ToUpper().PadRight(7)}] [{threadId}] [{source}] {message}{macInfo}";
        
        _logQueue.Enqueue(line);
        ProcessQueue();
    }

    private static void ProcessQueue()
    {
        lock (_fileLock)
        {
            if (_isProcessing) return;
            _isProcessing = true;
        }

        Task.Run(() =>
        {
            try
            {
                while (_logQueue.TryDequeue(out string? line))
                {
                    lock (_fileLock)
                    {
                        CheckRotation();
                        File.AppendAllText(CurrentLogFile, line + Environment.NewLine);
                    }
                }
            }
            catch { }
            finally
            {
                lock (_fileLock) { _isProcessing = false; }
            }
        });
    }

    private static void CheckRotation()
    {
        try
        {
            var fileInfo = new FileInfo(CurrentLogFile);
            if (fileInfo.Exists && fileInfo.Length > 10 * 1024 * 1024) // 10MB
            {
                string archivePath = Path.Combine(LogDir, $"noderadar_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                File.Move(CurrentLogFile, archivePath);
            }
        }
        catch { }
    }

    private static void CleanupOldLogs()
    {
        try
        {
            var logs = Directory.GetFiles(LogDir, "*.log")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Skip(10) // Keep last 10 log files
                .ToList();

            foreach (var file in logs)
            {
                if (file.FullName == CurrentLogFile) continue;
                file.Delete();
            }
        }
        catch { }
    }

    public static string GetLogDirectory() => LogDir;
}
