using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using NodeRadarPro.Core;

namespace Core.Tests
{
    public class ArpResolverTests
    {
        [Fact]
        public void ResolveMacAddress_WithInvalidIp_ReturnsUnknown()
        {
            string result = ArpResolver.ResolveMacAddress("invalid_ip");
            Assert.Equal("Unknown", result);
        }

        [Fact]
        public void ResolveMacAddress_WithEmptyIp_ReturnsUnknown()
        {
            string result = ArpResolver.ResolveMacAddress(string.Empty);
            Assert.Equal("Unknown", result);
        }

        [Fact]
        public void TryResolveNetBiosName_WithInvalidIp_ReturnsEmptyString()
        {
            string result = ArpResolver.TryResolveNetBiosName("invalid_ip");
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void TryResolveNetBiosName_WithBlackholeIp_ReturnsEmptyStringAndRespectsTimeout()
        {
            string blackholeIp = "192.0.2.1"; // TEST-NET-1
            var sw = Stopwatch.StartNew();
            string result = ArpResolver.TryResolveNetBiosName(blackholeIp);
            sw.Stop();
            Assert.Equal(string.Empty, result);
            Assert.True(sw.ElapsedMilliseconds < 5000, "Timeout was not respected.");
        }

        [Fact]
        public void GetFullArpTable_ReturnsListAndDoesNotThrow()
        {
            var result = ArpResolver.GetFullArpTable();
            Assert.NotNull(result);
            Assert.IsType<List<(string Ip, string Mac)>>(result);
        }
    }
}
