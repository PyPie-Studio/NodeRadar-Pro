using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Engines.Abstractions;

public interface IArpResolver
{
    string ResolveMacAddress(string ip, string sourceIp = "");
    List<(string Ip, string Mac)> GetFullArpTable();
    string TryResolveNetBiosName(string ipAddress);
    Task<string> TryResolveNetBiosNameAsync(string ipAddress, CancellationToken cancellationToken = default)
        => ArpResolver.TryResolveNetBiosNameAsync(ipAddress, cancellationToken: cancellationToken);
    bool TryGetMacFromArpTable(string ip, out string mac)
    {
        var list = GetFullArpTable();
        var found = list.Find(x => x.Ip == ip);
        if (!string.IsNullOrEmpty(found.Mac))
        {
            mac = found.Mac;
            return true;
        }
        mac = "Unknown";
        return false;
    }
    IReadOnlyDictionary<string, string> GetFullArpTableAsDictionary()
    {
        var list = GetFullArpTable();
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in list)
        {
            dict[item.Ip] = item.Mac;
        }
        return dict;
    }
}
