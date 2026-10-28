using System.Collections.Concurrent;
using LiteDB;
using Moq;
using NodeRadarPro.Core;
using NodeRadarPro.Data;
using NodeRadarPro.UI;

namespace NodeRadarPro.Tests.UI.Pages;

[Collection("AvaloniaUIStaticState")]
public class ScannerPageHeadlessTests : IDisposable
{
    private readonly MemoryStream _ms;
    private readonly LiteDatabase _liteDb;
    private readonly LocalDatabase _db;
    private readonly SubnetScanner _scanner;
    private readonly ConcurrentDictionary<string, NetworkNode> _activeNodes;

    public ScannerPageHeadlessTests()
    {
        AvaloniaTestApp.EnsureInitialized();
        _ms = new MemoryStream();
        _liteDb = new LiteDatabase(_ms);
        _db = new LocalDatabase(_liteDb);

        var mockPing = new Mock<IPingProvider>();
        var mockArp = new Mock<IArpResolver>();
        mockArp.Setup(a => a.GetFullArpTableAsDictionary()).Returns(new Dictionary<string, string>());
        _scanner = new SubnetScanner(mockPing.Object, mockArp.Object);
        _activeNodes = new ConcurrentDictionary<string, NetworkNode>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _liteDb.Dispose();
        _ms.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ScannerPage_InitialState_ButtonsAndInputsConfigured()
    {
        var page = new ScannerPage(_db, _scanner, _activeNodes);

        Assert.NotNull(page.StartButton);
        Assert.NotNull(page.StopButton);
        Assert.NotEmpty(page.StartIpInput.Text ?? "");
        Assert.NotEmpty(page.EndIpInput.Text ?? "");
    }

    [Theory]
    [InlineData("192.168.1.1", "192.168.2.1", "Start and end IPs must share the same /24 subnet prefix.")]
    [InlineData("192.168.1.200", "192.168.1.50", "Start IP host octet must be less than or equal to end IP.")]
    [InlineData("invalid-ip", "192.168.1.254", "Invalid IPv4 address format.")]
    [InlineData("192.168.1.1", "invalid-ip", "Invalid IPv4 address format.")]
    public void ScannerPage_IpRangeValidation_SetsAppropriateStatusMessage(string startIp, string endIp, string expectedErrorSubstring)
    {
        var page = new ScannerPage(_db, _scanner, _activeNodes);
        page.StartIpInput.Text = startIp;
        page.EndIpInput.Text = endIp;

        page.TriggerStartScan();

        Assert.Contains(expectedErrorSubstring, page.StatusText.Text);
    }

    [Fact]
    public void ScannerPage_CheckBoxToggles_ReflectState()
    {
        var page = new ScannerPage(_db, _scanner, _activeNodes);

        page.FastScanCheckBox.IsChecked = true;
        Assert.True(page.FastScanCheckBox.IsChecked);

        page.FastScanCheckBox.IsChecked = false;
        Assert.False(page.FastScanCheckBox.IsChecked);

        page.OsDetectCheckBox.IsChecked = true;
        Assert.True(page.OsDetectCheckBox.IsChecked);
    }
}
