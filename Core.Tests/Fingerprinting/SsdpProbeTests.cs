using System.Reflection;
using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting.Probes;
using Xunit;

namespace Core.Tests.Fingerprinting
{
    public class SsdpProbeTests
    {
        [Fact]
        public void ProbeProperties_ReturnExpectedValues()
        {
            var probe = new SsdpProbe();
            Assert.Equal("SSDP / UPnP", probe.Name);
            Assert.Equal(30, probe.Priority);
        }

        [Fact]
        public async Task ProbeAsync_WhenNotInCache_ReturnsEmptyRawData()
        {
            var probe = new SsdpProbe();
            var node = new NetworkNode { IpAddress = "192.168.1.254" };

            var result = await probe.ProbeAsync(node, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("SSDP / UPnP", result.Source);
            Assert.Empty(result.RawData);
        }

        [Fact]
        public async Task ProbeAsync_WhenInCache_PopulatesRawData()
        {
            var cacheField = typeof(SsdpProbe).GetField("_cache", BindingFlags.Static | BindingFlags.NonPublic);
            var cache = cacheField?.GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<string, SsdpProbe.SsdpData>;
            Assert.NotNull(cache);

            string testIp = "192.168.1.50";
            var ssdpData = new SsdpProbe.SsdpData
            {
                Server = "Linux/4.14 UPnP/1.0",
                FriendlyName = "Living Room TV",
                Manufacturer = "Samsung",
                ModelName = "Smart TV 2023",
                DeviceType = "urn:schemas-upnp-org:device:MediaRenderer:1"
            };
            cache[testIp] = ssdpData;

            var probe = new SsdpProbe();
            var node = new NetworkNode { IpAddress = testIp };

            var result = await probe.ProbeAsync(node, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("SSDP / UPnP", result.Source);
            Assert.Equal("Linux/4.14 UPnP/1.0", result.RawData["Server"]);
            Assert.Equal("Living Room TV", result.RawData["FriendlyName"]);
            Assert.Equal("Samsung", result.RawData["Manufacturer"]);
            Assert.Equal("Smart TV 2023", result.RawData["ModelName"]);
            Assert.Equal("urn:schemas-upnp-org:device:MediaRenderer:1", result.RawData["DeviceType"]);
        }

        [Fact]
        public void ExtractXmlValue_ParsesValidTagsAndHandlesInvalid()
        {
            var method = typeof(SsdpProbe).GetMethod("ExtractXmlValue", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);

            string xml = "<root><friendlyName> Test Device </friendlyName><manufacturer></manufacturer></root>";

            string fn = (string)method.Invoke(null, new object[] { xml, "friendlyName" })!;
            string mfg = (string)method.Invoke(null, new object[] { xml, "manufacturer" })!;
            string missing = (string)method.Invoke(null, new object[] { xml, "modelName" })!;

            Assert.Equal("Test Device", fn);
            Assert.Equal("", mfg);
            Assert.Equal("", missing);
        }
    }
}
