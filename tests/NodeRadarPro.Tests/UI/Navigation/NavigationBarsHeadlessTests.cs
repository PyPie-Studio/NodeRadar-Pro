namespace NodeRadarPro.Tests.UI.Navigation;

[Collection("AvaloniaUIStaticState")]
public class NavigationBarsHeadlessTests
{
    public NavigationBarsHeadlessTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void SideNavBar_InitialState_SelectsDashboard()
    {
        var navBar = new SideNavBar();

        Assert.Equal("dashboard", navBar.ActivePage);
        Assert.NotEmpty(navBar.NavItems);
        Assert.True(navBar.NavItems.ContainsKey("dashboard"));
        Assert.True(navBar.NavItems.ContainsKey("inventory"));
        Assert.True(navBar.NavItems.ContainsKey("settings"));
    }

    [Fact]
    public void SideNavBar_TriggerItemClick_FiresPageChangedAndUpdatesActive()
    {
        var navBar = new SideNavBar();
        string? selectedPage = null;
        navBar.PageChanged += page => selectedPage = page;

        navBar.TriggerItemClick("inventory");

        Assert.Equal("inventory", navBar.ActivePage);
        Assert.Equal("inventory", selectedPage);
    }

    [Fact]
    public void TopNavBar_UpdateStatus_RendersHealthAndLatencyStates()
    {
        var topBar = new TopNavBar();

        // Stable
        topBar.UpdateStatus(onlineCount: 5, avgLatency: 18, alertCount: 0);
        Assert.Contains("Stable", topBar.HealthText.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Latency: 18ms", topBar.LatencyText.Text);

        // Alerts active
        topBar.UpdateStatus(onlineCount: 5, avgLatency: 22, alertCount: 3);
        Assert.Contains("3 Alert(s)", topBar.HealthText.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Latency: 22ms", topBar.LatencyText.Text);

        // No devices
        topBar.UpdateStatus(onlineCount: 0, avgLatency: -1, alertCount: 0);
        Assert.Contains("No Devices", topBar.HealthText.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Latency: —", topBar.LatencyText.Text);
    }
}
