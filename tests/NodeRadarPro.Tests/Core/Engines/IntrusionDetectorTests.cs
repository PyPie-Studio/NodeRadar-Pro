using System.Net.NetworkInformation;
using System.Net.Sockets;
using LiteDB;
using Moq;
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
        GC.SuppressFinalize(this);
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
        var existing = new NetworkNode { MacAddress = "00:11:22:33:44:01", IpAddress = "192.168.1.1", DeviceName = "Known Device" };

        // Pre-insert known node directly into database
        _db.MergeWithHistory(existing);

        var mockPing = new Mock<IPingProvider>();
        mockPing.Setup(p => p.SendPingAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new PingReplyWrapper { Status = IPStatus.TimedOut, RoundtripTime = 0 });
        mockPing.Setup(p => p.SendPingAsync("192.168.1.1", It.IsAny<int>()))
            .ReturnsAsync(new PingReplyWrapper { Status = IPStatus.Success, RoundtripTime = 5 });
        mockPing.Setup(p => p.SendPingAsync("192.168.1.2", It.IsAny<int>()))
            .ReturnsAsync(new PingReplyWrapper { Status = IPStatus.Success, RoundtripTime = 8 });

        var mockArp = new Mock<IArpResolver>();
        mockArp.Setup(a => a.ResolveMacAddress(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(ArpResolver.UnknownMac);
        mockArp.Setup(a => a.ResolveMacAddress("192.168.1.1", It.IsAny<string>()))
            .Returns("00:11:22:33:44:01");
        mockArp.Setup(a => a.ResolveMacAddress("192.168.1.2", It.IsAny<string>()))
            .Returns("00:11:22:33:44:02");
        mockArp.Setup(a => a.GetFullArpTable())
            .Returns(new List<(string Ip, string Mac)>
            {
                ("192.168.1.1", "00:11:22:33:44:01"),
                ("192.168.1.2", "00:11:22:33:44:02")
            });

        var mockTcp = new Mock<ITcpPortProber>();
        mockTcp.Setup(t => t.ProbePortAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var realScanner = new SubnetScanner(mockPing.Object, mockArp.Object, mockTcp.Object)
        {
            TimeoutMs = 1,
            EnableDnsResolve = false,
            EnableInlinePortScan = false,
            EnableOsDetection = false,
            ScanAllSubnets = false
        };

        var detector = new IntrusionDetector(realScanner, _db, node => alertedNodes.Add(node))
        {
            BaseIpOverride = "192.168.1"
        };

        // Act - execute actual detector sweep with real SubnetScanner
        await detector.SweepOnceAsync();

        // Assert - only the new device is alerted
        Assert.Single(alertedNodes);
        Assert.Equal("00:11:22:33:44:02", alertedNodes[0].MacAddress);

        // Verify database persistence of both nodes
        var allTracked = _db.GetAllDevices();
        Assert.Equal(2, allTracked.Count);
        Assert.Contains(allTracked, d => d.MacAddress == "00:11:22:33:44:01");
        Assert.Contains(allTracked, d => d.MacAddress == "00:11:22:33:44:02");

        // Verify alert event persistence
        var alerts = _db.GetAlerts();
        Assert.Single(alerts);
        Assert.Equal(AlertType.NewDeviceDiscovered, alerts[0].AlertType);
        Assert.Equal("00:11:22:33:44:02", alerts[0].MacAddress);
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
    [Fact]
    public async Task SweepOnceAsync_InsertsAlertWithUtcTimestamp()
    {
        var mockPing = new Mock<IPingProvider>();
        mockPing.Setup(p => p.SendPingAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new PingReplyWrapper { Status = IPStatus.Success, RoundtripTime = 5 });

        var mockArp = new Mock<IArpResolver>();
        mockArp.Setup(a => a.ResolveMacAddress(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("00:AA:BB:CC:DD:EE");
        mockArp.Setup(a => a.GetFullArpTable())
            .Returns(new List<(string Ip, string Mac)> { ("192.168.1.50", "00:AA:BB:CC:DD:EE") });

        var mockTcp = new Mock<ITcpPortProber>();
        mockTcp.Setup(t => t.ProbePortAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var scanner = new SubnetScanner(mockPing.Object, mockArp.Object, mockTcp.Object)
        {
            TimeoutMs = 1,
            EnableDnsResolve = false,
            EnableInlinePortScan = false,
            EnableOsDetection = false,
            ScanAllSubnets = false
        };

        var detector = new IntrusionDetector(scanner, _db)
        {
            BaseIpOverride = "192.168.1"
        };

        var beforeSweep = DateTime.UtcNow.AddSeconds(-1);
        await detector.SweepOnceAsync();
        var afterSweep = DateTime.UtcNow.AddSeconds(1);

        var alerts = _db.GetAlerts();
        Assert.NotEmpty(alerts);
        var alert = alerts.First(a => a.MacAddress == "00:AA:BB:CC:DD:EE");
        var utcTimestamp = alert.Timestamp.ToUniversalTime();
        Assert.True(utcTimestamp >= beforeSweep && utcTimestamp <= afterSweep);
    }
}
