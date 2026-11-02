using LiteDB;
using NodeRadarPro.Data;

namespace NodeRadarPro.Tests.UI.Pages;

[Collection("AvaloniaUIStaticState")]
public class SystemLogsPageHeadlessTests : IDisposable
{
    private readonly MemoryStream _ms;
    private readonly LiteDatabase _liteDb;
    private readonly LocalDatabase _db;

    public SystemLogsPageHeadlessTests()
    {
        AvaloniaTestApp.EnsureInitialized();
        _ms = new MemoryStream();
        _liteDb = new LiteDatabase(_ms);
        _db = new LocalDatabase(_liteDb);
    }

    public void Dispose()
    {
        _db.Dispose();
        _liteDb.Dispose();
        _ms.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void SystemLogsPage_EmptyDatabase_InitializesZeroCounts()
    {
        var page = new SystemLogsPage(_db);

        Assert.Equal("0", page.InfoStatValue.Text);
        Assert.Equal("0", page.WarnStatValue.Text);
        Assert.Equal("0", page.ErrorStatValue.Text);
        Assert.Contains("0 entries", page.EntryCountText.Text, StringComparison.OrdinalIgnoreCase);
        Assert.True(page.ExportButton.IsEnabled);
    }

    [Fact]
    public void SystemLogsPage_WithEntries_UpdatesTelemetryCounts()
    {
        _db.Log(LogLevel.Info, "Scanner", "Subnet scan started");
        _db.Log(LogLevel.Warning, "Arp", "High latency node detected");
        _db.Log(LogLevel.Error, "Security", "Unauthorized MAC detected");

        var page = new SystemLogsPage(_db);
        page.TriggerRefreshLogs();

        Assert.Equal("1", page.InfoStatValue.Text);
        Assert.Equal("1", page.WarnStatValue.Text);
        Assert.Equal("1", page.ErrorStatValue.Text);
        Assert.Contains("3 entries", page.EntryCountText.Text, StringComparison.OrdinalIgnoreCase);
    }
}
