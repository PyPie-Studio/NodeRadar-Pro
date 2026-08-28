using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

public class AudioServiceTests : IDisposable
{
    public AudioServiceTests()
    {
        AudioService.Enabled = true;
    }

    public void Dispose()
    {
        AudioService.Enabled = true;
    }

    [Fact]
    public void Enabled_CanBeSetAndRead()
    {
        AudioService.Enabled = false;
        Assert.False(AudioService.Enabled);

        AudioService.Enabled = true;
        Assert.True(AudioService.Enabled);
    }

    [Fact]
    public void PlayAlert_WhenDisabled_DoesNotThrow()
    {
        AudioService.Enabled = false;
        var ex1 = Record.Exception(() => AudioService.PlayAlert(true));
        var ex2 = Record.Exception(() => AudioService.PlayAlert(false));
        AudioService.Enabled = true;

        Assert.Null(ex1);
        Assert.Null(ex2);
    }

    [Fact]
    public void PlayAlert_WhenEnabled_HandlesExceptionsGracefully()
    {
        AudioService.Enabled = true;
        var ex1 = Record.Exception(() => AudioService.PlayAlert(true));
        var ex2 = Record.Exception(() => AudioService.PlayAlert(false));
        Assert.Null(ex1);
        Assert.Null(ex2);
    }
}
