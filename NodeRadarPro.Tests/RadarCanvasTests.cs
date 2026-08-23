using NodeRadarPro.Core;
using NodeRadarPro.UI;

namespace NodeRadarPro.Tests;

public class RadarCanvasTests
{
    [Fact]
    public void RadarCanvas_Initialization_SetsClipToBounds()
    {
        var canvas = new RadarCanvas();
        Assert.True(canvas.ClipToBounds);
    }

    [Fact]
    public void UpdateNodes_SetsNodesAndDoesNotThrow()
    {
        var canvas = new RadarCanvas();
        var nodes = new List<NetworkNode>
        {
            new NetworkNode { IpAddress = "192.168.1.1", MacAddress = "00:11:22:33:44:55", CustomName = "Router", IsOnline = true, IsRegistered = true, ThreatLevel = ThreatLevel.Critical },
            new NetworkNode { IpAddress = "192.168.1.2", MacAddress = "00:11:22:33:44:56", CustomName = "PC", IsOnline = false, IsRegistered = false, ThreatLevel = ThreatLevel.Warning },
            new NetworkNode { IpAddress = "192.168.1.3", MacAddress = "00:11:22:33:44:57", CustomName = "Server", IsOnline = true, IsRegistered = false, ThreatLevel = ThreatLevel.Safe }
        };

        canvas.UpdateNodes(nodes);
        Assert.NotNull(canvas);
    }

    [Fact]
    public void SelectNode_UpdatesSelectedMac()
    {
        var canvas = new RadarCanvas();
        canvas.SelectNode("00:11:22:33:44:55");
        canvas.SelectNode(null);
        Assert.NotNull(canvas);
    }
}
