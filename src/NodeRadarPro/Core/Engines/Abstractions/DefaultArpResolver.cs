using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Engines.Abstractions;

public class DefaultArpResolver : IArpResolver
{
    public string ResolveMacAddress(string ip, string sourceIp = "") => ArpResolver.ResolveMacAddress(ip, sourceIp);
    public List<(string Ip, string Mac)> GetFullArpTable() => ArpResolver.GetFullArpTable();
    public string TryResolveNetBiosName(string ipAddress) => ArpResolver.TryResolveNetBiosName(ipAddress);
    public Task<string> TryResolveNetBiosNameAsync(string ipAddress, CancellationToken cancellationToken = default)
        => ArpResolver.TryResolveNetBiosNameAsync(ipAddress, cancellationToken: cancellationToken);
    public bool TryGetMacFromArpTable(string ip, out string mac) => ArpResolver.TryGetMacFromArpTable(ip, out mac);
    public IReadOnlyDictionary<string, string> GetFullArpTableAsDictionary() => ArpResolver.GetFullArpTableAsDictionary();
}
