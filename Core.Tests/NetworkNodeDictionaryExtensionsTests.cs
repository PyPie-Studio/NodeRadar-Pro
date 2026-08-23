using System.Collections.Concurrent;
using NodeRadarPro.Core;
using Xunit;

namespace Core.Tests;

public class NetworkNodeDictionaryExtensionsTests
{
    [Fact]
    public void UpdateNode_NewNode_AddsToDictionary()
    {
        var dict = new ConcurrentDictionary<string, NetworkNode>();
        var node = new NetworkNode
        {
            MacAddress = "AA:BB:CC:DD:EE:FF",
            IpAddress = "192.168.1.50",
            IsOnline = true
        };

        dict.UpdateNode(node);

        Assert.True(dict.ContainsKey("AA:BB:CC:DD:EE:FF"));
        Assert.Equal(node, dict["AA:BB:CC:DD:EE:FF"]);
    }

    [Fact]
    public void UpdateNode_ExistingNode_UpdatesInDictionary()
    {
        var dict = new ConcurrentDictionary<string, NetworkNode>();
        var oldNode = new NetworkNode
        {
            MacAddress = "AA:BB:CC:DD:EE:FF",
            IpAddress = "192.168.1.50",
            IsOnline = false
        };
        dict.TryAdd(oldNode.MacAddress, oldNode);

        var updatedNode = new NetworkNode
        {
            MacAddress = "AA:BB:CC:DD:EE:FF",
            IpAddress = "192.168.1.50",
            IsOnline = true,
            PingLatencyMs = 12
        };

        dict.UpdateNode(updatedNode);

        Assert.Single(dict);
        Assert.True(dict["AA:BB:CC:DD:EE:FF"].IsOnline);
        Assert.Equal(12, dict["AA:BB:CC:DD:EE:FF"].PingLatencyMs);
    }
}
