using System.Diagnostics;

namespace NodeRadarPro.Tests.Core.Engines;

public class ArpResolverTests
{
    [Fact]
    public void ResolveMacAddress_InvalidIp_ReturnsUnknown()
    {
        // Arrange
        string invalidIp = "invalid-ip";

        // Act
        string result = ArpResolver.ResolveMacAddress(invalidIp);

        // Assert
        Assert.Equal("Unknown", result);
    }

    [Fact]
    public void ResolveMacAddress_NullOrEmptyIp_ReturnsUnknown()
    {
        // Act & Assert
        Assert.Equal("Unknown", ArpResolver.ResolveMacAddress(null!));
        Assert.Equal("Unknown", ArpResolver.ResolveMacAddress(string.Empty));
    }

    [Fact]
    public void TryResolveNetBiosName_InvalidIp_ReturnsEmptyString()
    {
        // Arrange
        string invalidIp = "invalid-ip";

        // Act
        string result = ArpResolver.TryResolveNetBiosName(invalidIp);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void TryResolveNetBiosName_NonResponsiveIp_TimesOutAndReturnsEmptyString()
    {
        // Arrange
        string nonResponsiveIp = "192.0.2.1"; // TEST-NET-1 IP address

        // Act
        string result = ArpResolver.TryResolveNetBiosName(nonResponsiveIp, timeoutMs: 25);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task TryResolveNetBiosNameAsync_InvalidIp_ReturnsEmptyString()
    {
        // Arrange
        string invalidIp = "invalid-ip";

        // Act
        string result = await ArpResolver.TryResolveNetBiosNameAsync(invalidIp);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task TryResolveNetBiosNameAsync_NonResponsiveIp_TimesOutAndReturnsEmptyString()
    {
        // Arrange
        string nonResponsiveIp = "192.0.2.1"; // TEST-NET-1 IP address

        // Act
        string result = await ArpResolver.TryResolveNetBiosNameAsync(nonResponsiveIp, timeoutMs: 25);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetFullArpTable_ReturnsListWithoutExceptions()
    {
        // Act
        var result = ArpResolver.GetFullArpTable();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void ResolveMacAddress_SendArpException_ReturnsUnknown()
    {
        // Arrange
        string validIp = "192.168.1.1";
        static int failingSendArp(int destIp, int srcIp, byte[] pMacAddr, ref uint phyAddrLen)
        {
            throw new Exception("Simulated SendARP failure");
        }

        // Act
        string result = ArpResolver.ResolveMacAddress(validIp, "", failingSendArp);

        // Assert
        Assert.Equal("Unknown", result);
    }

    [Fact]
    public void GetWindowsArpTable_DoesNotUseExternalProcess()
    {
        var dictionary = ArpResolver.GetFullArpTableAsDictionary();
        Assert.NotNull(dictionary);
        Assert.DoesNotContain(Process.GetProcessesByName("arp"), p => !p.HasExited);
    }

    [Fact]
    public void TryParseProcNetArpLine_ValidLine_ParsesIpAndMac()
    {
        string line = "192.168.1.50     0x1         0x2         00-11-22-33-44-55     *        eth0";
        bool parsed = ArpResolver.TryParseProcNetArpLine(line, out ReadOnlySpan<char> ipSpan, out ReadOnlySpan<char> macSpan);

        Assert.True(parsed);
        Assert.Equal("192.168.1.50", ipSpan.ToString());
        Assert.Equal("00-11-22-33-44-55", macSpan.ToString());
    }

    [Fact]
    public void TryParseProcNetArpLine_InvalidLine_ReturnsFalse()
    {
        string line = "InvalidLineWithoutSpaces";
        bool parsed = ArpResolver.TryParseProcNetArpLine(line, out _, out _);

        Assert.False(parsed);
    }

    [Fact]
    public void FormatMacSpan_FormatsDashesToColonsAndUppercase()
    {
        ReadOnlySpan<char> rawMac = "0e-6f-31-e3-e5-dc".AsSpan();
        string formatted = ArpResolver.FormatMacSpan(rawMac);

        Assert.Equal("0E:6F:31:E3:E5:DC", formatted);
    }

    [Fact]
    public void TryGetMacFromArpTable_NullOrEmptyIp_ReturnsFalseAndUnknown()
    {
        bool foundNull = ArpResolver.TryGetMacFromArpTable(null!, out string macNull);
        Assert.False(foundNull);
        Assert.Equal("Unknown", macNull);

        bool foundEmpty = ArpResolver.TryGetMacFromArpTable(string.Empty, out string macEmpty);
        Assert.False(foundEmpty);
        Assert.Equal("Unknown", macEmpty);
    }

    [Fact]
    public void TryGetMacFromArpTable_NonExistentIp_ReturnsFalseAndUnknown()
    {
        bool found = ArpResolver.TryGetMacFromArpTable("203.0.113.199", out string mac);
        Assert.False(found);
        Assert.Equal("Unknown", mac);
    }
    [Fact]
    public void GetWindowsArpTable_WithValidEntries_ParsesIpAndMacSuccessfully()
    {
        byte[] mac = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 };
        byte[] tableBuffer = BuildMibIpNetTableBuffer(("192.168.1.50", mac, 3));

        int mockDelegate(IntPtr ptr, ref int size, bool order)
        {
            if (ptr == IntPtr.Zero)
            {
                size = tableBuffer.Length;
                return 122; // ERROR_INSUFFICIENT_BUFFER
            }
            System.Runtime.InteropServices.Marshal.Copy(tableBuffer, 0, ptr, tableBuffer.Length);
            return 0; // NO_ERROR
        }

        var entries = ArpResolver.GetWindowsArpTable(mockDelegate);

        Assert.Single(entries);
        Assert.Equal("192.168.1.50", entries[0].Ip);
        Assert.Equal("00:11:22:33:44:55", entries[0].Mac);
    }

    [Fact]
    public void GetWindowsArpTable_WithInvalidAndSpecialEntries_FiltersCorrectly()
    {
        byte[] validMac = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
        byte[] invalidTypeMac = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x56 };
        byte[] broadcastMac = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        byte[] zeroMac = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        byte[] multicastMac = new byte[] { 0x01, 0x00, 0x5E, 0x00, 0x00, 0x01 };

        byte[] tableBuffer = BuildMibIpNetTableBuffer(
            ("192.168.1.51", invalidTypeMac, 2),  // Type 2 = invalid, should filter
            ("192.168.1.255", broadcastMac, 3),   // Broadcast MAC, should filter
            ("192.168.1.52", zeroMac, 3),         // All zeros MAC, should filter
            ("192.168.1.53", multicastMac, 3),    // Multicast MAC, should filter
            ("127.0.0.1", validMac, 3),           // Loopback IP, should filter
            ("224.0.0.1", validMac, 3),           // Multicast IP, should filter
            ("239.255.255.250", validMac, 3),     // Multicast IP, should filter
            ("192.168.1.99", validMac, 4)         // Valid static entry
        );

        int mockDelegate(IntPtr ptr, ref int size, bool order)
        {
            if (ptr == IntPtr.Zero)
            {
                size = tableBuffer.Length;
                return 122;
            }
            System.Runtime.InteropServices.Marshal.Copy(tableBuffer, 0, ptr, tableBuffer.Length);
            return 0;
        }

        var entries = ArpResolver.GetWindowsArpTable(mockDelegate);

        Assert.Single(entries);
        Assert.Equal("192.168.1.99", entries[0].Ip);
        Assert.Equal("AA:BB:CC:DD:EE:FF", entries[0].Mac);
    }

    [Fact]
    public void GetWindowsArpTable_WhenBufferNegotiationFails_ReturnsEmptyList()
    {
        static int failingDelegate(IntPtr ptr, ref int size, bool order)
        {
            return 5; // ERROR_ACCESS_DENIED
        }

        var entries = ArpResolver.GetWindowsArpTable(failingDelegate);
        Assert.Empty(entries);
    }

    [Fact]
    public void GetWindowsArpTable_WhenDelegateThrows_CatchesAndReturnsEmptyList()
    {
        static int throwingDelegate(IntPtr ptr, ref int size, bool order)
        {
            throw new InvalidOperationException("Simulated Win32 fault");
        }

        var entries = ArpResolver.GetWindowsArpTable(throwingDelegate);
        Assert.Empty(entries);
    }

    [Fact]
    public void GetFullArpTable_WithInjectedDelegate_BypassesCache()
    {
        byte[] mac = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC };
        byte[] tableBuffer = BuildMibIpNetTableBuffer(("10.0.0.15", mac, 3));

        int mockDelegate(IntPtr ptr, ref int size, bool order)
        {
            if (ptr == IntPtr.Zero)
            {
                size = tableBuffer.Length;
                return 122;
            }
            System.Runtime.InteropServices.Marshal.Copy(tableBuffer, 0, ptr, tableBuffer.Length);
            return 0;
        }

        var table = ArpResolver.GetFullArpTable(mockDelegate);
        Assert.Contains(table, entry => entry.Ip == "10.0.0.15" && entry.Mac == "12:34:56:78:9A:BC");
    }

    private static byte[] BuildMibIpNetTableBuffer(params (string Ip, byte[] Mac, int Type)[] entries)
    {
        int rowCount = entries.Length;
        int totalBytes = 4 + (rowCount * 24);
        byte[] buffer = new byte[totalBytes];

        BitConverter.GetBytes(rowCount).CopyTo(buffer, 0);

        for (int i = 0; i < rowCount; i++)
        {
            int rowOffset = 4 + (i * 24);
            var (ip, mac, type) = entries[i];

            BitConverter.GetBytes(i + 1).CopyTo(buffer, rowOffset);
            BitConverter.GetBytes(mac.Length).CopyTo(buffer, rowOffset + 4);
            Array.Copy(mac, 0, buffer, rowOffset + 8, Math.Min(mac.Length, 8));

            uint ipUint = BitConverter.ToUInt32(System.Net.IPAddress.Parse(ip).GetAddressBytes(), 0);
            BitConverter.GetBytes(ipUint).CopyTo(buffer, rowOffset + 16);
            BitConverter.GetBytes(type).CopyTo(buffer, rowOffset + 20);
        }

        return buffer;
    }
}
