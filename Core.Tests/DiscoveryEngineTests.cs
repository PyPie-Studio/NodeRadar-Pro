using System.Net;
using NodeRadarPro.Core.Discovery;

namespace Core.Tests
{
    public class DiscoveryEngineTests
    {
        private class FakeDiscoveryMethod : IDiscoveryMethod
        {
            public string Name { get; }
            private readonly Func<string, List<IPAddress>, Action<NetworkDevice>, CancellationToken, Task> _discoverFunc;

            public FakeDiscoveryMethod(string name, Func<string, List<IPAddress>, Action<NetworkDevice>, CancellationToken, Task> discoverFunc)
            {
                Name = name;
                _discoverFunc = discoverFunc;
            }

            public Task DiscoverAsync(string baseIp, List<IPAddress> targetIps, Action<NetworkDevice> onDeviceDiscovered, CancellationToken ct)
            {
                return _discoverFunc(baseIp, targetIps, onDeviceDiscovered, ct);
            }
        }

        [Fact]
        public async Task RunDiscoveryAsync_ParsesTargetIpsCorrectlyAndInvokesMethods()
        {
            // Arrange
            List<IPAddress>? capturedIps = null;
            var fakeMethod = new FakeDiscoveryMethod("TestProtocol", (baseIp, ips, onDevice, ct) =>
            {
                capturedIps = ips;
                return Task.CompletedTask;
            });

            var engine = new DiscoveryEngine(new[] { fakeMethod });

            // Act
            var results = await engine.RunDiscoveryAsync("192.168.1", 1, 3, _ => { }, CancellationToken.None);

            // Assert
            Assert.NotNull(capturedIps);
            Assert.Equal(3, capturedIps.Count);
            Assert.Equal(IPAddress.Parse("192.168.1.1"), capturedIps[0]);
            Assert.Equal(IPAddress.Parse("192.168.1.2"), capturedIps[1]);
            Assert.Equal(IPAddress.Parse("192.168.1.3"), capturedIps[2]);
            Assert.Empty(results);
        }

        [Fact]
        public async Task RunDiscoveryAsync_AggregatesAndMergesDevicesFromMultipleMethods()
        {
            // Arrange
            var discoveredCallbacks = new List<NetworkDevice>();

            var arpMethod = new FakeDiscoveryMethod("ARP", (baseIp, ips, onDevice, ct) =>
            {
                onDevice(new NetworkDevice
                {
                    IpAddress = "192.168.1.10",
                    MacAddress = "00:11:22:33:44:55",
                    Vendor = "Unknown Vendor",
                    Hostname = "Unknown Device",
                    DeviceType = "Generic Device",
                    SourceProtocol = "ARP",
                    RawDetails = "Arp entry"
                });
                return Task.CompletedTask;
            });

            var mdnsMethod = new FakeDiscoveryMethod("mDNS", (baseIp, ips, onDevice, ct) =>
            {
                onDevice(new NetworkDevice
                {
                    IpAddress = "192.168.1.10",
                    MacAddress = "Unknown",
                    Vendor = "Apple",
                    Hostname = "My-MacBook.local",
                    DeviceType = "Workstation",
                    SourceProtocol = "mDNS",
                    RawDetails = "mDNS entry"
                });
                return Task.CompletedTask;
            });

            var engine = new DiscoveryEngine(new[] { arpMethod, mdnsMethod });

            // Act
            var results = await engine.RunDiscoveryAsync("192.168.1", 10, 10, dev => discoveredCallbacks.Add(dev), CancellationToken.None);

            // Assert
            Assert.Single(results);
            var mergedDevice = results[0];
            Assert.Equal("192.168.1.10", mergedDevice.IpAddress);
            Assert.Equal("00:11:22:33:44:55", mergedDevice.MacAddress);
            Assert.Equal("Apple", mergedDevice.Vendor);
            Assert.Equal("My-MacBook.local", mergedDevice.Hostname);
            Assert.Equal("Workstation", mergedDevice.DeviceType);
            Assert.Contains("ARP", mergedDevice.SourceProtocol);
            Assert.Contains("mDNS", mergedDevice.SourceProtocol);
            Assert.NotEmpty(discoveredCallbacks);
        }

        [Fact]
        public async Task RunDiscoveryAsync_IgnoresDeviceWithNullOrEmptyIp()
        {
            // Arrange
            var method = new FakeDiscoveryMethod("BadIpMethod", (baseIp, ips, onDevice, ct) =>
            {
                onDevice(new NetworkDevice { IpAddress = "", Hostname = "Invalid" });
                return Task.CompletedTask;
            });

            var engine = new DiscoveryEngine(new[] { method });

            // Act
            var results = await engine.RunDiscoveryAsync("192.168.1", 1, 2, _ => { }, CancellationToken.None);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task RunDiscoveryAsync_HandlesMethodExceptionGracefully()
        {
            // Arrange
            var failingMethod = new FakeDiscoveryMethod("FaultyMethod", (baseIp, ips, onDevice, ct) =>
            {
                throw new InvalidOperationException("Network socket failed");
            });

            var workingMethod = new FakeDiscoveryMethod("WorkingMethod", (baseIp, ips, onDevice, ct) =>
            {
                onDevice(new NetworkDevice { IpAddress = "192.168.1.5", Hostname = "AliveHost" });
                return Task.CompletedTask;
            });

            var engine = new DiscoveryEngine(new[] { failingMethod, workingMethod });

            // Act
            var results = await engine.RunDiscoveryAsync("192.168.1", 1, 10, _ => { }, CancellationToken.None);

            // Assert
            Assert.Single(results);
            Assert.Equal("192.168.1.5", results[0].IpAddress);
        }

        [Fact]
        public async Task RunDiscoveryAsync_HandlesCancellationGracefully()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var cancellingMethod = new FakeDiscoveryMethod("CancelledMethod", (baseIp, ips, onDevice, ct) =>
            {
                cts.Cancel();
                ct.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

            var engine = new DiscoveryEngine(new[] { cancellingMethod });

            // Act & Assert (Should not throw OperationCanceledException out of RunDiscoveryAsync)
            var results = await engine.RunDiscoveryAsync("192.168.1", 1, 5, _ => { }, cts.Token);
            Assert.Empty(results);
        }
    }
}
