using System.Diagnostics;
using NodeRadarPro.Core;
using NodeRadarPro.Data;

namespace NodeRadarPro.Tests.Data;

public class DatabaseBackupServiceBenchmarkTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dbPath;
    private readonly LocalDatabase _db;

    public DatabaseBackupServiceBenchmarkTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DatabaseBackupServiceBenchmarkTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _dbPath = Path.Combine(_tempDir, "bench_noderadar.db");
        _db = new LocalDatabase(_dbPath, "testpass");
    }

    public void Dispose()
    {
        try
        {
            _db.DisposeConnection();
        }
        catch { }

        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch { }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Benchmark_CleanupOldBackups_NPlusOneVsBatched()
    {
        int fileCount = 50;

        // Warmup
        _db.Log(LogLevel.Info, "Database", "Warmup");

        // --- Baseline Simulation: Individual Log() Calls (N+1 inserts) ---
        var swBaseline = Stopwatch.StartNew();
        for (int i = 0; i < fileCount; i++)
        {
            _db.Log(LogLevel.Info, "Database", $"Cleaned up old backup: noderadar_backup_baseline_{i}.db");
        }
        swBaseline.Stop();
        long baselineMs = swBaseline.ElapsedMilliseconds;

        // --- Batched LogBulk Calls (1 InsertBulk) ---
        var logItems = new List<(LogLevel level, string source, string message)>(fileCount);
        for (int i = 0; i < fileCount; i++)
        {
            logItems.Add((LogLevel.Info, "Database", $"Cleaned up old backup: noderadar_backup_batched_{i}.db"));
        }

        var swBatched = Stopwatch.StartNew();
        _db.LogBulk(logItems);
        swBatched.Stop();
        long batchedMs = swBatched.ElapsedMilliseconds;

        // Verify all logs were recorded in database
        var logs = _db.GetLogs(limit: 1000);
        Assert.True(logs.Count >= fileCount * 2);

        // Verify speedup / output benchmark metrics
        double speedup = batchedMs > 0 ? (double)baselineMs / batchedMs : baselineMs;
        Assert.True(swBatched.ElapsedTicks <= swBaseline.ElapsedTicks || batchedMs <= baselineMs + 50);
    }
}
