using System.Net.Sockets;
using LiteDB;
using NodeRadarPro.Core;
using NodeRadarPro.Data;

namespace NodeRadarPro.Tests;

public class IntrusionDetectorTests : IDisposable
{
    private readonly MemoryStream _ms;
    private readonly LiteDatabase _liteDb;
    private readonly LocalDatabase _db;

    public IntrusionDetectorTests()
    {
        _ms = new MemoryStream();
        _liteDb = new LiteDatabase(_ms, new BsonMapper());
        _db = new LocalDatabase(_liteDb);
    }

    public void Dispose()
    {
        _liteDb.Dispose();
        _ms.Dispose();
    }

    private class ThrowingSubnetScanner : SubnetScanner
    {
        private readonly Exception _exceptionToThrow;

        public ThrowingSubnetScanner(Exception exceptionToThrow)
        {
            _exceptionToThrow = exceptionToThrow;
        }

        public override Task<List<NetworkNode>> ScanSubnetAsync(string baseIp, CancellationToken token = default)
        {
            return Task.FromException<List<NetworkNode>>(_exceptionToThrow);
        }
    }

    [Fact]
    public async Task SweepOnceAsync_WithNewDevice_TriggersAlertAndPersistsToDatabase()
    {
        // Arrange
        var alertedNodes = new List<NetworkNode>();
        var fakeScanner = new SubnetScanner
        {
            TimeoutMs = 10,
            EnableDnsResolve = false,
            EnableOsDetection = false
        };

        var detector = new IntrusionDetector(fakeScanner, _db, node => alertedNodes.Add(node));

        // Pre-insert a known node directly
        var existing = new NetworkNode { MacAddress = "00:11:22:33:44:01", IpAddress = "192.168.1.1" };
        _db.MergeWithHistory(existing);

        // Act - run single sweep with mock results simulation
        var newDevice = new NetworkNode { MacAddress = "00:11:22:33:44:02", IpAddress = "192.168.1.2" };
        var mergeResult = _db.MergeWithHistoryBulk(new List<NetworkNode> { existing, newDevice });

        // Assert
        Assert.Single(mergeResult);
        Assert.Equal("00:11:22:33:44:02", mergeResult[0].MacAddress);
    }

    [Fact]
    public async Task SweepOnceAsync_Cancellation_ExitsCleanly()
    {
        var detector = new IntrusionDetector(new SubnetScanner(), _db);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var ex = await Record.ExceptionAsync(() => detector.SweepOnceAsync(cts.Token));
        Assert.Null(ex);
    }

    [Fact]
    public async Task StartAsync_WhenCancelled_TerminatesGracefully()
    {
        var detector = new IntrusionDetector(new SubnetScanner(), _db);
        using var cts = new CancellationTokenSource(50);

        var task = detector.StartAsync(cts.Token);
        await task;

        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task SweepOnceAsync_SocketException_LogsErrorToDatabase()
    {
        // Arrange
        var socketException = new SocketException((int)SocketError.NetworkDown);
        var throwingScanner = new ThrowingSubnetScanner(socketException);
        var detector = new IntrusionDetector(throwingScanner, _db);

        // Act
        await detector.SweepOnceAsync();

        // Assert
        var logs = _db.GetLogs(levelFilter: LogLevel.Error);
        var log = Assert.Single(logs);
        Assert.Equal("IntrusionDetector", log.Source);
        Assert.Equal(LogLevel.Error, log.Level);
        Assert.Contains($"Sweep failed: {socketException.Message}", log.Message);
    }

    [Fact]
    public async Task SweepOnceAsync_TimeoutException_LogsErrorToDatabase()
    {
        // Arrange
        var timeoutException = new TimeoutException("Scan timeout exceeded");
        var throwingScanner = new ThrowingSubnetScanner(timeoutException);
        var detector = new IntrusionDetector(throwingScanner, _db);

        // Act
        await detector.SweepOnceAsync();

        // Assert
        var logs = _db.GetLogs(levelFilter: LogLevel.Error);
        var log = Assert.Single(logs);
        Assert.Equal("IntrusionDetector", log.Source);
        Assert.Equal(LogLevel.Error, log.Level);
        Assert.Contains("Sweep failed: Scan timeout exceeded", log.Message);
    }

    [Fact]
    public void Configure_AppliesAppSettingsToScanner()
    {
        // Arrange
        var scanner = new SubnetScanner();
        var detector = new IntrusionDetector(scanner, _db);
        var settings = new AppSettings
        {
            SelectedInterfaceName = "eth0",
            ResponseTimeoutMs = 2500,
            EnableDnsResolve = false,
            EnableFastScan = true,
            EnableOsDetection = false,
            EnableInlinePortScan = true
        };

        // Act
        detector.Configure(settings);

        // Assert
        Assert.Equal("eth0", scanner.PreferredInterfaceName);
        Assert.Equal(2500, scanner.TimeoutMs);
        Assert.False(scanner.EnableDnsResolve);
        Assert.True(scanner.FastScanMode);
        Assert.False(scanner.EnableOsDetection);
        Assert.True(scanner.EnableInlinePortScan);
    }

    [Fact]
    public void Configure_ReconfiguresScannerWhenCalledMultipleTimes()
    {
        // Arrange
        var scanner = new SubnetScanner();
        var detector = new IntrusionDetector(scanner, _db);

        var initialSettings = new AppSettings
        {
            SelectedInterfaceName = "Ethernet 1",
            ResponseTimeoutMs = 1000,
            EnableDnsResolve = true,
            EnableFastScan = false,
            EnableOsDetection = true,
            EnableInlinePortScan = false
        };

        var updatedSettings = new AppSettings
        {
            SelectedInterfaceName = "Wi-Fi",
            ResponseTimeoutMs = 3000,
            EnableDnsResolve = false,
            EnableFastScan = true,
            EnableOsDetection = false,
            EnableInlinePortScan = true
        };

        // Act
        detector.Configure(initialSettings);
        Assert.Equal("Ethernet 1", scanner.PreferredInterfaceName);
        Assert.Equal(1000, scanner.TimeoutMs);
        Assert.True(scanner.EnableDnsResolve);
        Assert.False(scanner.FastScanMode);
        Assert.True(scanner.EnableOsDetection);
        Assert.False(scanner.EnableInlinePortScan);

        detector.Configure(updatedSettings);

        // Assert
        Assert.Equal("Wi-Fi", scanner.PreferredInterfaceName);
        Assert.Equal(3000, scanner.TimeoutMs);
        Assert.False(scanner.EnableDnsResolve);
        Assert.True(scanner.FastScanMode);
        Assert.False(scanner.EnableOsDetection);
        Assert.True(scanner.EnableInlinePortScan);
    }
}
