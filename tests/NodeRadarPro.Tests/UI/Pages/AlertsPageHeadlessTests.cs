using Avalonia.Controls;
using LiteDB;
using NodeRadarPro.Data;

namespace NodeRadarPro.Tests.UI.Pages;

[Collection("AvaloniaUIStaticState")]
public class AlertsPageHeadlessTests : IDisposable
{
    private readonly MemoryStream _ms;
    private readonly LiteDatabase _liteDb;
    private readonly LocalDatabase _db;

    public AlertsPageHeadlessTests()
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
    public void AlertsPage_EmptyDatabase_DisplaysHealthyMessage()
    {
        var page = new AlertsPage(_db);

        Assert.Equal("0", page.ActiveCountText.Text);
        Assert.Equal("0", page.ResolvedCountText.Text);
        Assert.Equal("0", page.TotalCountText.Text);
        Assert.Single(page.AlertListBody.Children);

        var emptyTb = Assert.IsType<TextBlock>(page.AlertListBody.Children[0]);
        Assert.Contains("healthy", emptyTb.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AlertsPage_WithAlerts_CalculatesActiveResolvedTotalCounts()
    {
        _db.InsertAlert(new AlertEvent { DeviceName = "HostA", AlertType = AlertType.ConnectionLost, IsResolved = false, Message = "Lost ping response" });
        _db.InsertAlert(new AlertEvent { DeviceName = "HostB", AlertType = AlertType.HighLatency, IsResolved = false, Message = "Spike 450ms" });
        _db.InsertAlert(new AlertEvent { DeviceName = "HostC", AlertType = AlertType.DeviceReconnected, IsResolved = true, Message = "Restored" });

        var page = new AlertsPage(_db);

        Assert.Equal("2", page.ActiveCountText.Text);
        Assert.Equal("1", page.ResolvedCountText.Text);
        Assert.Equal("3", page.TotalCountText.Text);
        Assert.Equal(2, page.AlertListBody.Children.Count);
    }

    [Fact]
    public void AlertsPage_FilterModeSwitching_FiltersAlertCards()
    {
        _db.InsertAlert(new AlertEvent { DeviceName = "HostA", AlertType = AlertType.ConnectionLost, IsResolved = false });
        _db.InsertAlert(new AlertEvent { DeviceName = "HostB", AlertType = AlertType.HighLatency, IsResolved = false });
        _db.InsertAlert(new AlertEvent { DeviceName = "HostC", AlertType = AlertType.DeviceReconnected, IsResolved = true });

        var page = new AlertsPage(_db);

        // Resolved mode
        page.SetFilterMode("resolved");
        Assert.Single(page.AlertListBody.Children);

        // All mode
        page.SetFilterMode("all");
        Assert.Equal(3, page.AlertListBody.Children.Count);

        // Active mode
        page.SetFilterMode("active");
        Assert.Equal(2, page.AlertListBody.Children.Count);
    }

    [Fact]
    public void AlertsPage_ResolveAll_ResolvesActiveAlertsAndUpdatesCounts()
    {
        _db.InsertAlert(new AlertEvent { DeviceName = "HostA", AlertType = AlertType.ConnectionLost, IsResolved = false });
        _db.InsertAlert(new AlertEvent { DeviceName = "HostB", AlertType = AlertType.HighLatency, IsResolved = false });

        var page = new AlertsPage(_db);
        Assert.Equal("2", page.ActiveCountText.Text);

        page.TriggerResolveAll();

        Assert.Equal("0", page.ActiveCountText.Text);
        Assert.Equal("2", page.ResolvedCountText.Text);
        Assert.Equal("2", page.TotalCountText.Text);
    }

    [Fact]
    public void AlertsPage_ClearResolved_TwoStepConfirmationDeletesResolved()
    {
        _db.InsertAlert(new AlertEvent { DeviceName = "HostA", AlertType = AlertType.ConnectionLost, IsResolved = false });
        _db.InsertAlert(new AlertEvent { DeviceName = "HostB", AlertType = AlertType.DeviceReconnected, IsResolved = true });

        var page = new AlertsPage(_db);
        Assert.Equal("1", page.ResolvedCountText.Text);

        // First click arms latch
        page.TriggerClearResolved();
        Assert.True(page.ClearResolvedButton.IsEnabled);

        // Second click executes clear
        page.TriggerClearResolved();
        Assert.Equal("0", page.ResolvedCountText.Text);
        Assert.Equal("1", page.ActiveCountText.Text);
    }

    [Fact]
    public void AlertsPage_ClearAll_TwoStepConfirmationClearsAllAlerts()
    {
        _db.InsertAlert(new AlertEvent { DeviceName = "HostA", AlertType = AlertType.ConnectionLost, IsResolved = false });
        _db.InsertAlert(new AlertEvent { DeviceName = "HostB", AlertType = AlertType.DeviceReconnected, IsResolved = true });

        var page = new AlertsPage(_db);
        Assert.Equal("2", page.TotalCountText.Text);

        // First click arms latch
        page.TriggerClearAll();
        Assert.True(page.ClearAllButton.IsEnabled);

        // Second click executes clear
        page.TriggerClearAll();
        Assert.Equal("0", page.TotalCountText.Text);
        Assert.Equal("0", page.ActiveCountText.Text);
        Assert.Equal("0", page.ResolvedCountText.Text);
    }
}
