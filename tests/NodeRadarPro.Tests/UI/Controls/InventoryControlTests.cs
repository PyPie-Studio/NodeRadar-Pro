using NodeRadarPro.UI.Controls.Inventory;

namespace NodeRadarPro.Tests.UI.Controls;

[Collection("AvaloniaUIStaticState")]
public class InventoryControlTests
{
    public InventoryControlTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void InventoryFilterBar_FilterSelection_UpdatesActiveAndFiresEvent()
    {
        var filterBar = new InventoryFilterBar();
        string? active = null;
        filterBar.FilterChanged += f => active = f;

        filterBar.SetActiveFilter("servers");

        Assert.Equal("servers", filterBar.ActiveFilter);
        Assert.Equal("servers", active);
    }

    [Fact]
    public void InventoryFilterBar_SetDeviceCount_UpdatesText()
    {
        var filterBar = new InventoryFilterBar();
        filterBar.SetDeviceCount(42);
        Assert.Equal("(42)", filterBar.DeviceCountText.Text);
    }

    [Fact]
    public void InventoryGridControl_RenderCards_PopulatesDeviceCards()
    {
        var grid = new InventoryGridControl();
        var nodes = new List<NetworkNode>
        {
            new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:01", IpAddress = "192.168.1.10", Hostname = "Host-1", IsOnline = true },
            new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:02", IpAddress = "192.168.1.20", Hostname = "Host-2", IsOnline = false, IsRegistered = true }
        };

        grid.RenderCards(nodes);

        Assert.Equal(2, grid.DeviceListContainer.Children.Count);
    }

    [Fact]
    public void InventoryGridControl_SelectAll_PopulatesSelectedMacs()
    {
        var grid = new InventoryGridControl();
        var nodes = new List<NetworkNode>
        {
            new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:01", IpAddress = "192.168.1.10", Hostname = "Host-1" },
            new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:02", IpAddress = "192.168.1.20", Hostname = "Host-2" }
        };

        grid.SelectAll(nodes, true);

        Assert.Equal(2, grid.SelectedMacs.Count);
        Assert.Contains("AA:BB:CC:DD:EE:01", grid.SelectedMacs);
        Assert.Contains("AA:BB:CC:DD:EE:02", grid.SelectedMacs);

        grid.SelectAll(nodes, false);
        Assert.Empty(grid.SelectedMacs);
    }
}
