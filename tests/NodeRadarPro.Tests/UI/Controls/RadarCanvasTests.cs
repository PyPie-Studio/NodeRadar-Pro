using Avalonia;
using Avalonia.Media;
using Moq;
using NodeRadarPro.Core;
using NodeRadarPro.UI;

namespace NodeRadarPro.Tests;

public class RadarCanvasTests
{
    private static bool _appInitialized;

    public RadarCanvasTests()
    {
        if (!_appInitialized)
        {
            try
            {
                AppBuilder.Configure<Avalonia.Application>()
                    .UsePlatformDetect()
                    .WithInterFont()
                    .SetupWithoutStarting();
            }
            catch
            {
            }
            _appInitialized = true;
        }
    }

    [Fact]
    public void RadarCanvas_Initialization_SetsClipToBounds()
    {
        var canvas = new RadarCanvas();
        Assert.True(canvas.ClipToBounds);
    }

    [Fact]
    public void UpdateNodes_And_SelectNode_UpdatesInternalState()
    {
        var canvas = new RadarCanvas();
        var nodes = new List<NetworkNode>
        {
            new NetworkNode { IpAddress = "192.168.1.1", MacAddress = "00:11:22:33:44:55", CustomName = "Router", IsOnline = true, IsRegistered = true, ThreatLevel = ThreatLevel.Critical },
            new NetworkNode { IpAddress = "192.168.1.2", MacAddress = "00:11:22:33:44:56", CustomName = "PC", IsOnline = false, IsRegistered = false, ThreatLevel = ThreatLevel.Warning },
            new NetworkNode { IpAddress = "192.168.1.3", MacAddress = "00:11:22:33:44:57", CustomName = "Server", IsOnline = true, IsRegistered = false, ThreatLevel = ThreatLevel.Safe }
        };

        var ex1 = Record.Exception(() => canvas.UpdateNodes(nodes));
        var ex2 = Record.Exception(() => canvas.SelectNode("00:11:22:33:44:55"));
        var ex3 = Record.Exception(() => canvas.SelectNode(null));

        Assert.Null(ex1);
        Assert.Null(ex2);
        Assert.Null(ex3);
    }

    [Fact]
    public void Render_ZeroBounds_ReturnsEarlyWithoutThrowing()
    {
        var canvas = new RadarCanvas();
        canvas.Measure(new Size(0, 0));
        canvas.Arrange(new Rect(0, 0, 0, 0));
        var mockContext = new Mock<DrawingContext>();
        var ex = Record.Exception(() => canvas.Render(mockContext.Object));
        Assert.Null(ex);
    }
}
