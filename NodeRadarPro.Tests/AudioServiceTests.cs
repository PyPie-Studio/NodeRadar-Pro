using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

public class AudioServiceTests
{
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
        var exception = Record.Exception(() => AudioService.PlayAlert(false));
        Assert.Null(exception);
    }
}
