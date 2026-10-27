using NodeRadarPro.Core;
using NodeRadarPro.UI;

namespace NodeRadarPro.Tests.UI.Pages;

[Collection("AvaloniaUIStaticState")]
public class DashboardPageHeadlessTests
{
    public DashboardPageHeadlessTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void DashboardPage_InitialState_RendersOnlineCountsAndAverageLatency()
    {
        var nodes = new List<NetworkNode>
        {
            new NetworkNode { IpAddress = "192.168.1.10", IsOnline = true, PingLatencyMs = 20 },
            new NetworkNode { IpAddress = "192.168.1.20", IsOnline = true, PingLatencyMs = 40 },
            new NetworkNode { IpAddress = "192.168.1.30", IsOnline = false, PingLatencyMs = -1 }
        };

        var page = new DashboardPage(nodes);
        page.RefreshData();

        Assert.Equal("2", page.OnlineCountText.Text);
        Assert.Equal("30ms", page.LatencyValueText.Text);
    }

    [Fact]
    public void DashboardPage_NoResponsiveNodes_ShowsDashLatency()
    {
        var nodes = new List<NetworkNode>
        {
            new NetworkNode { IpAddress = "192.168.1.30", IsOnline = false, PingLatencyMs = -1 }
        };

        var page = new DashboardPage(nodes);
        page.RefreshData();

        Assert.Equal("0", page.OnlineCountText.Text);
        Assert.Equal("—", page.LatencyValueText.Text);
    }

    [Fact]
    public void DashboardPage_HealthGauge_CalculatesStableStatus()
    {
        var nodes = new List<NetworkNode>
        {
            new NetworkNode { IpAddress = "192.168.1.10", IsOnline = true, PingLatencyMs = 15 },
            new NetworkNode { IpAddress = "192.168.1.20", IsOnline = true, PingLatencyMs = 25 }
        };

        var page = new DashboardPage(nodes);
        page.RefreshData();

        Assert.Equal("100%", page.HealthPercentText.Text);
        Assert.Equal("STABLE", page.HealthStatusText.Text);
    }

    [Fact]
    public void DashboardPage_PingsList_PopulatesRecentOnlineNodes()
    {
        var nodes = new List<NetworkNode>
        {
            new NetworkNode { IpAddress = "192.168.1.10", IsOnline = true, PingLatencyMs = 12, LastSeen = DateTime.UtcNow },
            new NetworkNode { IpAddress = "192.168.1.20", IsOnline = true, PingLatencyMs = 24, LastSeen = DateTime.UtcNow.AddMinutes(-1) },
            new NetworkNode { IpAddress = "192.168.1.30", IsOnline = false, IsRegistered = true, LastSeen = DateTime.UtcNow.AddMinutes(-5) }
        };

        var page = new DashboardPage(nodes);
        page.RefreshData();

        Assert.Equal(3, page.PingsList.Children.Count);
    }

    [Fact]
    public void DashboardPage_SetNodes_UpdatesMetricsDynamically()
    {
        var initialNodes = new List<NetworkNode>
        {
            new NetworkNode { IpAddress = "192.168.1.10", IsOnline = true, PingLatencyMs = 50 }
        };

        var page = new DashboardPage(initialNodes);
        page.RefreshData();
        Assert.Equal("1", page.OnlineCountText.Text);
        Assert.Equal("50ms", page.LatencyValueText.Text);

        var updatedNodes = new List<NetworkNode>
        {
            new NetworkNode { IpAddress = "192.168.1.10", IsOnline = true, PingLatencyMs = 10 },
            new NetworkNode { IpAddress = "192.168.1.20", IsOnline = true, PingLatencyMs = 20 }
        };

        page.SetNodes(updatedNodes);
        Assert.Equal("2", page.OnlineCountText.Text);
        Assert.Equal("15ms", page.LatencyValueText.Text);
    }
}
