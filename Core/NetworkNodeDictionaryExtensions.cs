using System.Collections.Concurrent;

namespace NodeRadarPro.Core;

public static class NetworkNodeDictionaryExtensions
{
    /// <summary>
    /// Helper to cleanly add or update a node in a ConcurrentDictionary without repeating the update lambda.
    /// Uses the MAC address as the key.
    /// </summary>
    public static void UpdateNode(this ConcurrentDictionary<string, NetworkNode> map, NetworkNode node)
    {
        map.AddOrUpdate(node.MacAddress, node, (_, _) => node);
    }
}
