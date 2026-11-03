namespace NodeRadarPro.Tests.UI.Pages;

[Collection("AvaloniaUIStaticState")]
public class PortScansPageHeadlessTests
{
    public PortScansPageHeadlessTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void PortScansPage_InitialState_ConfiguresControlsAndButtons()
    {
        var page = new PortScansPage();

        Assert.NotNull(page.TargetIpInput);
        Assert.Equal($"{NodeRadarPro.Core.Engines.SubnetScanner.GetLocalBaseIp()}.1", page.TargetIpInput.Text);
        Assert.Equal("1", page.StartPortInput.Text);
        Assert.Equal("1024", page.EndPortInput.Text);
        Assert.True(page.ScanButton.IsEnabled);
        Assert.True(page.StopButton.IsEnabled);
        Assert.Equal("0%", page.ProgressPct.Text);
        Assert.Equal("OPEN PORTS: 0", page.OpenCountText.Text);
        Assert.Contains("Ready to scan", page.StatusText.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PortScansPage_ResultsBody_InitializesEmpty()
    {
        var page = new PortScansPage();

        Assert.NotNull(page.ResultsBody);
        Assert.Empty(page.ResultsBody.Children);
    }
}
