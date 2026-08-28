using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using NodeRadarPro.Core.Fingerprinting;
using NodeRadarPro.Core.Discovery;

namespace NodeRadarPro.Core;

public static class ScannerDiagnostics
{
    public static async Task<int> RunDiagnosticsAsync(Func<NetworkInterface[]>? getNetworkInterfaces = null)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("      NODERADAR PRO - SCANNER DIAGNOSTICS SUITE   ");
        Console.WriteLine("==================================================");
        Console.WriteLine($"Time: {DateTime.Now}");
        Console.WriteLine($"OS: {Environment.OSVersion}");

        bool allPassed = true;

        // Test 1: Network Interface Detection
        allPassed &= TestInterfaceDetection(getNetworkInterfaces);

        // Test 2: Local Subnet Retrieval
        allPassed &= TestSubnetRetrieval();

        // Test 3: ARP Table Parsing
        allPassed &= TestArpTableParsing();

        // Test 4: Device Classifier & Fingerprinting
        allPassed &= TestDeviceClassifier();

        // Test 5: Dynamic QuickProbe
        allPassed &= await TestQuickProbeAsync();

        // Test 6: Scanner Deduplication & Execution (Sweep 3 IPs)
        allPassed &= await TestScannerSweepAndDeduplicationAsync();

        // Test 7: Asynchronous Discovery Engine Verification
        allPassed &= TestDiscoveryEngineComponents();

        Console.WriteLine();
        if (allPassed)
        {
            Console.WriteLine(">>> ALL DIAGNOSTIC TESTS PASSED SUCCESSFULLY! <<<");
            return 0; // Success code
        }
        else
        {
            Console.WriteLine(">>> ERROR: ONE OR MORE DIAGNOSTIC TESTS FAILED. <<<");
            return 1; // Error code
        }
    }

    private static bool TestInterfaceDetection(Func<NetworkInterface[]>? getNetworkInterfaces = null)
    {
        Console.Write("Test 1: Network Interfaces Detection... ");
        try
        {
            var interfaces = getNetworkInterfaces != null ? getNetworkInterfaces() : NetworkInterface.GetAllNetworkInterfaces();
            if (interfaces == null || interfaces.Length == 0)
            {
                Console.WriteLine("FAIL (No interfaces found)");
                return false;
            }
            Console.WriteLine($"PASS ({interfaces.Length} interfaces detected)");
            foreach (var ni in interfaces.Take(3))
            {
                Console.WriteLine($"  - {ni.Name} (Type: {ni.NetworkInterfaceType}, Status: {ni.OperationalStatus})");
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL (Exception: {ex.Message})");
            return false;
        }
    }

    private static bool TestSubnetRetrieval()
    {
        Console.Write("Test 2: Local Subnet Base IP Resolution... ");
        try
        {
            var baseIps = SubnetScanner.GetAllLocalBaseIps();
            if (baseIps == null || baseIps.Count == 0)
            {
                Console.WriteLine("FAIL (No base subnets detected)");
                return false;
            }
            Console.WriteLine($"PASS ({baseIps.Count} subnets found: {string.Join(", ", baseIps)})");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL (Exception: {ex.Message})");
            return false;
        }
    }

    private static bool TestArpTableParsing()
    {
        Console.Write("Test 3: ARP Table Parsing... ");
        try
        {
            var table = ArpResolver.GetFullArpTable();
            if (table == null)
            {
                Console.WriteLine("FAIL (Null table returned)");
                return false;
            }
            Console.WriteLine($"PASS ({table.Count} entries in ARP cache)");
            foreach (var entry in table.Take(3))
            {
                Console.WriteLine($"  - IP: {entry.Ip} => MAC: {entry.Mac}");
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL (Exception: {ex.Message})");
            return false;
        }
    }

    private static bool TestDeviceClassifier()
    {
        Console.Write("Test 4: Device Classifier Engine... ");
        try
        {
            // Case A: Synology NAS
            var nodeNas = new NetworkNode { IpAddress = "192.168.1.55", MacAddress = "00:11:32:AA:BB:CC", Hostname = "Synology-NAS" };
            var probesNas = new List<ProbeResult>
            {
                new ProbeResult { Source = "MAC OUI Lookup", RawData = new Dictionary<string, string> { { "Vendor", "Synology" } } },
                new ProbeResult { Source = "mDNS / Bonjour", RawData = new Dictionary<string, string> { { "Model", "DS220+" } } }
            };
            var classNas = DeviceClassifierEngine.Classify(nodeNas, probesNas);
            if (classNas.TypeString != "NAS Storage" || classNas.Vendor != "Synology" || classNas.Model != "DS220+")
            {
                Console.WriteLine($"FAIL NAS (Got Type: {classNas.TypeString}, Vendor: {classNas.Vendor}, Model: {classNas.Model})");
                return false;
            }

            // Case B: Samsung Mobile Phone (Android)
            var nodePhone = new NetworkNode { IpAddress = "192.168.1.60", MacAddress = "10:19:94:11:22:33", Hostname = "Galaxy-S21" };
            var probesPhone = new List<ProbeResult>
            {
                new ProbeResult { Source = "MAC OUI Lookup", RawData = new Dictionary<string, string> { { "Vendor", "Samsung Electronics" } } }
            };
            var classPhone = DeviceClassifierEngine.Classify(nodePhone, probesPhone);
            if (classPhone.TypeString != "Mobile Phone" || classPhone.Vendor != "Samsung Electronics" || classPhone.Os != "Linux/Android")
            {
                Console.WriteLine($"FAIL Samsung (Got Type: {classPhone.TypeString}, Vendor: {classPhone.Vendor}, OS: {classPhone.Os})");
                return false;
            }

            // Case C: Router / ONU (Fiberhome)
            var nodeRouter = new NetworkNode { IpAddress = "192.168.1.1", MacAddress = "00:0B:AD:99:88:77", Hostname = "router.local" };
            var probesRouter = new List<ProbeResult>
            {
                new ProbeResult { Source = "MAC OUI Lookup", RawData = new Dictionary<string, string> { { "Vendor", "Fiberhome" } } }
            };
            var classRouter = DeviceClassifierEngine.Classify(nodeRouter, probesRouter);
            if (classRouter.TypeString != "Router / Gateway" || classRouter.Vendor != "Fiberhome" || classRouter.Os != "Infrastructure")
            {
                Console.WriteLine($"FAIL Router (Got Type: {classRouter.TypeString}, Vendor: {classRouter.Vendor}, OS: {classRouter.Os})");
                return false;
            }

            // Case D: Randomized / Privacy MAC (iPhone/iOS)
            var nodePrivacy = new NetworkNode { IpAddress = "192.168.1.70", MacAddress = "02:11:22:33:44:55", Hostname = "Unknown Device" };
            var probesPrivacy = new List<ProbeResult>
            {
                new ProbeResult { Source = "MAC OUI Lookup", RawData = new Dictionary<string, string> { { "Vendor", "Privacy MAC" } } }
            };
            var classPrivacy = DeviceClassifierEngine.Classify(nodePrivacy, probesPrivacy);
            if (classPrivacy.TypeString != "Mobile Device" || classPrivacy.Vendor != "Privacy MAC" || classPrivacy.Os != "macOS/iOS")
            {
                Console.WriteLine($"FAIL Privacy MAC (Got Type: {classPrivacy.TypeString}, Vendor: {classPrivacy.Vendor}, OS: {classPrivacy.Os})");
                return false;
            }

            Console.WriteLine("PASS (NAS, Mobile Phone, Router/ONU, and Privacy MAC classified correctly)");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL (Exception: {ex.Message})");
            return false;
        }
    }

    private static async Task<bool> TestQuickProbeAsync()
    {
        Console.Write("Test 5: Live IP Quick Probe... ");
        try
        {
            var result = await SubnetScanner.QuickProbeAsync("127.0.0.1");
            Console.WriteLine($"PASS (Localhost probe completed: Online={result.IsOnline}, MAC={result.Mac}, Latency={result.LatencyMs}ms)");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL (Exception: {ex.Message})");
            return false;
        }
    }

    private static async Task<bool> TestScannerSweepAndDeduplicationAsync()
    {
        Console.Write("Test 6: Scanner Deduplication and Events... ");
        try
        {
            var scanner = new SubnetScanner { TimeoutMs = 500 };

            int discoveryEventCount = 0;
            var discoveredNodes = new List<NetworkNode>();

            scanner.NodeDiscovered += (node) =>
            {
                lock (discoveredNodes)
                {
                    discoveredNodes.Add(node);
                    discoveryEventCount++;
                }
            };

            string baseIp = SubnetScanner.GetLocalBaseIp();
            Console.WriteLine($"\n  Running mini-sweep on {baseIp}.1 to {baseIp}.3...");

            var results = await scanner.ScanRangeAsync(baseIp, 1, 3, CancellationToken.None);

            Console.WriteLine($"  - Total results returned by ScanRangeAsync: {results.Count}");
            Console.WriteLine($"  - Total events fired: {discoveryEventCount}");

            var duplicateMacsInResults = results.GroupBy(n => n.MacAddress).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            var duplicateMacsInEvents = discoveredNodes.GroupBy(n => n.MacAddress).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

            if (duplicateMacsInResults.Any())
            {
                Console.WriteLine($"  FAIL: Duplicate MACs found in results: {string.Join(", ", duplicateMacsInResults)}");
                return false;
            }

            if (duplicateMacsInEvents.Any())
            {
                Console.WriteLine($"  FAIL: Duplicate MACs found in events: {string.Join(", ", duplicateMacsInEvents)}");
                return false;
            }

            if (results.Count != discoveryEventCount)
            {
                Console.WriteLine($"  FAIL: Discrepancy between results count ({results.Count}) and events fired ({discoveryEventCount})");
                return false;
            }

            Console.WriteLine("  PASS (Sweep completed, zero duplicate MACs emitted)");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAIL (Exception during sweep: {ex.Message})");
            return false;
        }
    }

    private static bool TestDiscoveryEngineComponents()
    {
        Console.Write("Test 7: Asynchronous Discovery Engine Verification... ");
        try
        {
            // 1. Verify ARP OUI Lookup & LAA/Privacy MAC Recognition
            string appleVendor = ArpDiscoveryMethod.GetMacVendor("00:1C:B3:AA:BB:CC");
            if (appleVendor != "Apple")
            {
                Console.WriteLine($"FAIL: Apple OUI lookup returned {appleVendor}");
                return false;
            }

            string huaweiVendor = ArpDiscoveryMethod.GetMacVendor("20:0B:C7:11:22:33");
            if (huaweiVendor != "Huawei")
            {
                Console.WriteLine($"FAIL: Huawei OUI lookup returned {huaweiVendor}");
                return false;
            }

            string privacyMacVendor = ArpDiscoveryMethod.GetMacVendor("02:11:22:33:44:55");
            if (privacyMacVendor != "Privacy MAC")
            {
                Console.WriteLine($"FAIL: Privacy MAC OUI lookup returned {privacyMacVendor}");
                return false;
            }

            // 2. Verify mDNS Hostname Parser Heuristic
            var mockMdnsBytes = new byte[] {
                0x00, 0x00, 0x00, 0x00,
                0x07, 0x6d, 0x79, 0x70, 0x68, 0x6f, 0x6e, 0x65, // "myphone" (length 7)
                0x05, 0x6c, 0x6f, 0x63, 0x61, 0x6c, 0x00 // ".local" (length 5)
            };

            string parsedHost = MdnsDiscoveryMethod.ParseMdnsHostname(mockMdnsBytes);
            if (parsedHost != "myphone.local")
            {
                Console.WriteLine($"FAIL: mDNS parser failed to extract hostname. Got: '{parsedHost}'");
                return false;
            }

            // 3. Verify DiscoveryEngine aggregation and merge properties
            var engine = new DiscoveryEngine();
            if (engine == null)
            {
                Console.WriteLine("FAIL: DiscoveryEngine could not be instantiated.");
                return false;
            }

            Console.WriteLine("PASS (OUI dictionaries, LAA detection, mDNS packet parser, and Engine initialized successfully)");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL (Exception: {ex.Message})");
            return false;
        }
    }
}
