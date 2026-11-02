namespace NodeRadarPro.Tests.UI.Pages;

[Collection("AvaloniaUIStaticState")]
public class TraceroutePageHeadlessTests
{
    public TraceroutePageHeadlessTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void TraceroutePage_InitialState_InitializesControls()
    {
        var page = new TraceroutePage();

        Assert.NotNull(page.DestinationInput);
        Assert.Equal("8.8.8.8", page.DestinationInput.Text);
        Assert.True(page.StartButton.IsEnabled);
        Assert.False(page.StopButton.IsEnabled);
        Assert.Equal("Ready to map pathway.", page.StatusText.Text);
        Assert.Equal(0, page.ProgressBar.Value);
        Assert.NotNull(page.HopsContainer);
        Assert.Empty(page.HopsContainer.Children);
    }
}
