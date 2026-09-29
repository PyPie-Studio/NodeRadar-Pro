using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace NodeRadarPro.Core
{
    public interface IPingProvider
    {
        Task<PingReplyWrapper> SendPingAsync(string hostNameOrAddress, int timeout);
    }

    public class PingReplyWrapper
    {
        public IPStatus Status { get; set; }
        public long RoundtripTime { get; set; }
    }

    public class DefaultPingProvider : IPingProvider
    {
        public async Task<PingReplyWrapper> SendPingAsync(string hostNameOrAddress, int timeout)
        {
            using var pinger = new Ping();
            var reply = await pinger.SendPingAsync(hostNameOrAddress, timeout);
            return new PingReplyWrapper
            {
                Status = reply.Status,
                RoundtripTime = reply.RoundtripTime
            };
        }
    }

    public interface IArpResolver
    {
        string ResolveMacAddress(string ip, string sourceIp = "");
        List<(string Ip, string Mac)> GetFullArpTable();
        string TryResolveNetBiosName(string ipAddress);
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
    }

    public class DefaultArpResolver : IArpResolver
    {
        public string ResolveMacAddress(string ip, string sourceIp = "") => ArpResolver.ResolveMacAddress(ip, sourceIp);
        public List<(string Ip, string Mac)> GetFullArpTable() => ArpResolver.GetFullArpTable();
        public string TryResolveNetBiosName(string ipAddress) => ArpResolver.TryResolveNetBiosName(ipAddress);
        public bool TryGetMacFromArpTable(string ip, out string mac) => ArpResolver.TryGetMacFromArpTable(ip, out mac);
    }
}
