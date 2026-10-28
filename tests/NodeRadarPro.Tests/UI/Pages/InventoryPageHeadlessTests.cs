using Avalonia.Controls;
using LiteDB;
using Moq;
using NodeRadarPro.Core;
using NodeRadarPro.Data;
using NodeRadarPro.UI;

namespace NodeRadarPro.Tests.UI.Pages;

[Collection("AvaloniaUIStaticState")]
public class InventoryPageHeadlessTests : IDisposable
{
    private readonly MemoryStream _ms;
    private readonly LiteDatabase _liteDb;
    private readonly LocalDatabase _db;
    private readonly ConnectivityMonitor _monitor;

    public InventoryPageHeadlessTests()
    {
        AvaloniaTestApp.EnsureInitialized();
        _ms = new MemoryStream();
        _liteDb = new LiteDatabase(_ms);
        _db = new LocalDatabase(_liteDb);

        var mockPing = new Mock<IPingProvider>();
        var mockArp = new Mock<IArpResolver>();
        mockArp.Setup(a => a.GetFullArpTableAsDictionary()).Returns(new Dictionary<string, string>());
        _monitor = new ConnectivityMonitor(_db, mockPing.Object, mockArp.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
        _liteDb.Dispose();
        _ms.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void InventoryPage_InitialLoad_RendersCardsForActiveNodes()
    {
        var nodes = new List<NetworkNode>
        {
            new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:01", IpAddress = "192.168.1.10", Hostname = "Host-1", IsOnline = true },
            new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:02", IpAddress = "192.168.1.20", Hostname = "Host-2", IsOnline = false, IsRegistered = true }
        };

        var page = new InventoryPage(_db, _monitor, nodes);
        page.RefreshData();

        Assert.Equal("(2)", page.DeviceCountText.Text);
        Assert.Equal(2, page.DeviceListContainer.Children.Count);
    }

    [Fact]
    public void InventoryPage_SearchFilter_ReducesVisibleCards()
    {
        var nodes = new List<NetworkNode>
        {
            new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:01", IpAddress = "192.168.1.10", Hostname = "LaserPrinter", CustomName = "Office Printer", IsOnline = true },
            new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:02", IpAddress = "192.168.1.20", Hostname = "DatabaseServer", CustomName = "Main DB", IsOnline = true }
        };

        var page = new InventoryPage(_db, _monitor, nodes);
        page.RefreshData();
        Assert.Equal("(2)", page.DeviceCountText.Text);

        page.SearchBox.Text = "printer";
        page.RefreshData();

        Assert.Equal("(1)", page.DeviceCountText.Text);
        Assert.Single(page.DeviceListContainer.Children);
    }

    [Fact]
    public void InventoryPage_StatusFilters_FiltersCardsCorrectly()
    {
        var nodes = new List<NetworkNode>
        {
            new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:01", IpAddress = "192.168.1.10", Hostname = "OnlineNode", IsOnline = true },
            new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:02", IpAddress = "192.168.1.20", Hostname = "OfflineNode", IsOnline = false, IsRegistered = true }
        };

        var page = new InventoryPage(_db, _monitor, nodes);
        page.RefreshData();

        // Online filter
        page.SetActiveFilter("online");
        Assert.Equal("(1)", page.DeviceCountText.Text);
        Assert.Single(page.DeviceListContainer.Children);

        // Offline filter
        page.SetActiveFilter("offline");
        Assert.Equal("(1)", page.DeviceCountText.Text);
        Assert.Single(page.DeviceListContainer.Children);

        // Reset to all
        page.SetActiveFilter("all");
        Assert.Equal("(2)", page.DeviceCountText.Text);
        Assert.Equal(2, page.DeviceListContainer.Children.Count);
    }

    [Fact]
    public void InventoryPage_ToggleSelectionMode_EnablesSelectionControls()
    {
        var nodes = new List<NetworkNode>
        {
            new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:01", IpAddress = "192.168.1.10", Hostname = "Host-1", IsOnline = true }
        };

        var page = new InventoryPage(_db, _monitor, nodes);
        page.RefreshData();

        Assert.False(page.SelectAllCheckBox.IsVisible);

        // Raise click on Select Mode button
        page.SelectModeButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

        Assert.True(page.SelectAllCheckBox.IsVisible);
        Assert.Equal("Exit", page.SelectModeButton.Content);

        // Exit selection mode
        page.SelectModeButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.False(page.SelectAllCheckBox.IsVisible);
        Assert.Equal("Select", page.SelectModeButton.Content);
    }
}
