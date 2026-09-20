using NodeRadarPro.Core;
using NodeRadarPro.UI;

namespace NodeRadarPro.Tests.UI;

public class SupportPageAuditReportTests
{
    [Fact]
    public void BuildSecurityAuditReport_GeneratesExpectedSectionsAndContent()
    {
        // Arrange
        var devices = new List<NetworkNode>
        {
            new NetworkNode
            {
                IpAddress = "192.168.1.10",
                MacAddress = "00:11:22:33:44:55",
                CustomName = "Test Router",
                ThreatLevel = ThreatLevel.Safe,
                IsOnline = true,
                IsRegistered = true,
                OpenPorts = new List<int> { 80, 443 }
            },
            new NetworkNode
            {
                IpAddress = "192.168.1.50",
                MacAddress = "AA:BB:CC:DD:EE:FF",
                CustomName = "Suspicious Device",
                ThreatLevel = ThreatLevel.Critical,
                IsOnline = true,
                IsRegistered = false,
                OpenPorts = new List<int> { 22, 8080 }
            }
        };

        var alerts = new List<AlertEvent>
        {
            new AlertEvent
            {
                Timestamp = DateTime.Now,
                IsResolved = false,
                AlertType = AlertType.NewDeviceDiscovered,
                DeviceName = "Suspicious Device",
                Message = "Unregistered MAC detected on subnet."
            }
        };

        // Act
        string report = SupportPage.BuildSecurityAuditReport(devices, alerts);

        // Assert
        Assert.Contains("# NodeRadar Pro — Security & Reconnaissance Audit", report);
        Assert.Contains("## 1. Executive Summary", report);
        Assert.Contains("- **Total Tracked Devices:** 2", report);
        Assert.Contains("- **Online Devices:** 2 (100%)", report);
        Assert.Contains("- **Security Threat Devices:** 1 (1 Critical, 0 Warning)", report);
        Assert.Contains("- **Active / Unresolved Alerts:** 1", report);

        Assert.Contains("## 2. Threat Vector Summary", report);
        Assert.Contains("192.168.1.50", report);
        Assert.Contains("AA:BB:CC:DD:EE:FF", report);
        Assert.Contains("22, 8080", report);

        Assert.Contains("## 3. Discovered Network Assets", report);
        Assert.Contains("192.168.1.10", report);

        Assert.Contains("## 4. Recent Security Alerts", report);
        Assert.Contains("Unregistered MAC detected on subnet.", report);
    }

    [Fact]
    public void BuildSecurityAuditReport_HandlesEmptyLists()
    {
        // Arrange
        var devices = new List<NetworkNode>();
        var alerts = new List<AlertEvent>();

        // Act
        string report = SupportPage.BuildSecurityAuditReport(devices, alerts);

        // Assert
        Assert.Contains("# NodeRadar Pro — Security & Reconnaissance Audit", report);
        Assert.Contains("## 1. Executive Summary", report);
        Assert.Contains("- **Total Tracked Devices:** 0", report);
        Assert.Contains("## 3. Discovered Network Assets", report);
        Assert.DoesNotContain("## 2. Threat Vector Summary", report);
        Assert.DoesNotContain("## 4. Recent Security Alerts", report);
    }
}
