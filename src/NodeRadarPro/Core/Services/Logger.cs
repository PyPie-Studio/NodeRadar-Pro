using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace NodeRadarPro.Core;

/// <summary>
/// Professional rolling file logger for technical diagnostics.
/// Writes structured logs to %Documents%/PyPie Studio/NodeRadar Pro/Logs/
/// </summary>
public static class Logger
{
    private static readonly string _defaultLogDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PyPie Studio", "NodeRadar Pro", "Logs");
    private static string? _customLogDir;
    private static readonly object _fileLock = new();
    private static readonly ConcurrentQueue<string> _logQueue = new();
    private static bool _isProcessing = false;
    private static readonly ManualResetEventSlim _flushEvent = new(true);

    private static string LogDir => _customLogDir ?? _defaultLogDir;
    private static string CurrentLogFile => Path.Combine(LogDir, "noderadar_system.log");

    internal static void SetCustomLogDirectoryForTesting(string? customDir)
    {
        _customLogDir = customDir;
        if (!string.IsNullOrEmpty(customDir) && !Directory.Exists(customDir))
        {
            Directory.CreateDirectory(customDir);
        }
    }

    internal static void FlushForTesting()
    {
        while (!_logQueue.IsEmpty || _isProcessing)
        {
            _flushEvent.Wait(100);
        }
    }

    public static void Log(LogLevel level, string source, string message, string? deviceMac = null)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd  hh:mm:ss.fff tt");
        string threadId = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString().PadLeft(3, '0');
        string macInfo = !string.IsNullOrEmpty(deviceMac) ? $" [{deviceMac}]" : "";

        // Structure: [Timestamp] [Level] [Thread] [Source] Message [Device]
        string line = $"[{timestamp}] [{level.ToString().ToUpper(),-7}] [{threadId}] [{source}] {message}{macInfo}";

        _logQueue.Enqueue(line);
        ProcessQueue();
    }

    private static void ProcessQueue()
    {
        lock (_fileLock)
        {
            if (_isProcessing) return;
            _isProcessing = true;
            _flushEvent.Reset();
        }

        Task.Run(() =>
        {
            try
            {
                var batch = new List<string>(32);
                while (_logQueue.TryDequeue(out string? line))
                {
                    batch.Add(line);
                    if (batch.Count >= 64 || _logQueue.IsEmpty)
                    {
                        lock (_fileLock)
                        {
                            if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);
                            CheckRotation();
                            File.AppendAllLines(CurrentLogFile, batch);
                        }
                        batch.Clear();
                    }
                }
            }
            catch { }
            finally
            {
                lock (_fileLock)
                {
                    _isProcessing = false;
                    if (!_logQueue.IsEmpty)
                    {
                        ProcessQueue();
                    }
                    else
                    {
                        _flushEvent.Set();
                    }
                }
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
