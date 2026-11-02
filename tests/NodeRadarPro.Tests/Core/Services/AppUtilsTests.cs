using System.Net;
using System.Net.NetworkInformation;

namespace NodeRadarPro.Tests.Core.Services;

[Collection("LoggerStaticState")]
public class AppUtilsTests : IDisposable
{
    private readonly string _testLogDir;

    public AppUtilsTests()
    {
        _testLogDir = Path.Combine(Path.GetTempPath(), "NodeRadarAppUtilsTest_" + Guid.NewGuid());
        Directory.CreateDirectory(_testLogDir);
        Logger.SetCustomLogDirectoryForTesting(_testLogDir);
    }

    public void Dispose()
    {
        AppUtils.UrlLauncherForTesting = null;
        AppUtils.FolderLauncherForTesting = null;
        Logger.SetCustomLogDirectoryForTesting(null);
        try
        {
            if (Directory.Exists(_testLogDir))
                Directory.Delete(_testLogDir, true);
        }
        catch { }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void GetMacAddress_WithValidInterface_ReturnsFormattedMac()
    {
        var interfaces = new List<NetworkInterfaceInfo>
        {
            new NetworkInterfaceInfo
            {
                Name = "eth0",
                OperationalStatus = OperationalStatus.Up,
                NetworkInterfaceType = NetworkInterfaceType.Ethernet,
                PhysicalAddressBytes = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 }
            }
        };

        string mac = AppUtils.GetMacAddress(() => interfaces);
        Assert.Equal("00:11:22:33:44:55", mac);
    }

    [Fact]
    public void GetMacAddress_WithEmptyInterfaces_ReturnsUnknown()
    {
        string mac = AppUtils.GetMacAddress(() => new List<NetworkInterfaceInfo>());
        Assert.Equal("Unknown", mac);
    }

    [Fact]
    public void GetMacAddress_IgnoresDownInterfaces()
    {
        var interfaces = new List<NetworkInterfaceInfo>
        {
            new NetworkInterfaceInfo
            {
                Name = "eth0",
                OperationalStatus = OperationalStatus.Down,
                NetworkInterfaceType = NetworkInterfaceType.Ethernet,
                PhysicalAddressBytes = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 }
            }
        };

        string mac = AppUtils.GetMacAddress(() => interfaces);
        Assert.Equal("Unknown", mac);
    }

    [Fact]
    public void GetMacAddress_IgnoresLoopbackInterfaces()
    {
        var interfaces = new List<NetworkInterfaceInfo>
        {
            new NetworkInterfaceInfo
            {
                Name = "lo",
                OperationalStatus = OperationalStatus.Up,
                NetworkInterfaceType = NetworkInterfaceType.Loopback,
                PhysicalAddressBytes = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 }
            }
        };

        string mac = AppUtils.GetMacAddress(() => interfaces);
        Assert.Equal("Unknown", mac);
    }

    [Fact]
    public void GetMacAddress_IgnoresAllZeroMacAddresses()
    {
        var interfaces = new List<NetworkInterfaceInfo>
        {
            new NetworkInterfaceInfo
            {
                Name = "eth0",
                OperationalStatus = OperationalStatus.Up,
                NetworkInterfaceType = NetworkInterfaceType.Ethernet,
                PhysicalAddressBytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }
            }
        };

        string mac = AppUtils.GetMacAddress(() => interfaces);
        Assert.Equal("Unknown", mac);
    }

    [Fact]
    public void GetMacAddress_WhenExceptionThrown_ReturnsUnknownAndLogsError()
    {
        string exceptionMessage = "Network adapter error " + Guid.NewGuid();
        string mac = AppUtils.GetMacAddress(() => throw new InvalidOperationException(exceptionMessage));

        Assert.Equal("Unknown", mac);

        Logger.FlushForTesting();
        string logFile = Path.Combine(_testLogDir, "noderadar_system.log");
        Assert.True(File.Exists(logFile));
        string content = File.ReadAllText(logFile);
        Assert.Contains("AppUtils", content);
        Assert.Contains($"Failed to retrieve MAC address: {exceptionMessage}", content);
    }

    [Fact]
    public void GetLocalIpAddress_WithValidInterface_ReturnsIp()
    {
        var interfaces = new List<NetworkInterfaceInfo>
        {
            new NetworkInterfaceInfo
            {
                Name = "eth0",
                OperationalStatus = OperationalStatus.Up,
                NetworkInterfaceType = NetworkInterfaceType.Ethernet,
                UnicastAddresses = new List<IPAddress> { IPAddress.Parse("192.168.1.100") }
            }
        };

        string ip = AppUtils.GetLocalIpAddress(() => interfaces);
        Assert.Equal("192.168.1.100", ip);
    }

    [Fact]
    public void GetLocalIpAddress_WithEmptyInterfaces_ReturnsLoopback()
    {
        string ip = AppUtils.GetLocalIpAddress(() => new List<NetworkInterfaceInfo>());
        Assert.Equal("127.0.0.1", ip);
    }

    [Fact]
    public void GetLocalIpAddress_WhenExceptionThrown_ReturnsLoopback()
    {
        string ip = AppUtils.GetLocalIpAddress(() => throw new InvalidOperationException("Provider error"));
        Assert.Equal("127.0.0.1", ip);
    }

    [Fact]
    public void GetMacAddress_DefaultProvider_ExecutesWithoutThrowing()
    {
        string mac = AppUtils.GetMacAddress();
        Assert.NotNull(mac);
    }

    [Fact]
    public void GetLocalIpAddress_DefaultProvider_ExecutesWithoutThrowing()
    {
        string ip = AppUtils.GetLocalIpAddress();
        Assert.NotNull(ip);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    [InlineData("javascript:alert(1)")]
    public void OpenSafeUrl_InvalidOrNonHttpUrl_DoesNotThrow(string? invalidUrl)
    {
        var exception = Record.Exception(() => AppUtils.OpenSafeUrl(invalidUrl!));
        Assert.Null(exception);
    }

    [Fact]
    public void OpenSafeUrl_ValidHttpUrl_DoesNotThrow()
    {
        string? launchedUrl = null;
        AppUtils.UrlLauncherForTesting = url => launchedUrl = url;
        var exception = Record.Exception(() => AppUtils.OpenSafeUrl("https://example.com"));
        Assert.Null(exception);
        Assert.Equal("https://example.com/", launchedUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("C:\\NonExistentFolder_123456789")]
    public void OpenFolder_InvalidOrNonExistentFolder_DoesNotThrow(string? folderPath)
    {
        var exception = Record.Exception(() => AppUtils.OpenFolder(folderPath!));
        Assert.Null(exception);
    }

    [Fact]
    public void OpenFolder_ValidExistingFolder_DoesNotThrow()
    {
        string tempDir = Path.GetTempPath();
        string? launchedFolder = null;
        AppUtils.FolderLauncherForTesting = folder => launchedFolder = folder;
        var exception = Record.Exception(() => AppUtils.OpenFolder(tempDir));
        Assert.Null(exception);
        Assert.Equal(tempDir, launchedFolder);
    }

    [Theory]
    [InlineData("=1+1", "\"'=1+1\"")]
    [InlineData("+cmd|'/c calc'!A0", "\"'+cmd|'/c calc'!A0\"")]
    [InlineData("-5", "\"'-5\"")]
    [InlineData("@SUM(A1:A10)", "\"'@SUM(A1:A10)\"")]
    [InlineData("\tcmd", "\"'\tcmd\"")]
    [InlineData("Simple Text", "\"Simple Text\"")]
    [InlineData("Text with \"quotes\"", "\"Text with \"\"quotes\"\"\"")]
    [InlineData(null, "\"\"")]
    [InlineData("", "\"\"")]
    public void EscapeCsvCell_SanitizesFormulasAndQuotes(string? input, string expected)
    {
        string actual = AppUtils.EscapeCsvCell(input);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("Safe Text", "Safe Text")]
    [InlineData("Text | with | pipes", "Text \\| with \\| pipes")]
    [InlineData("Text <script>alert(1)</script>", "Text &lt;script&gt;alert(1)&lt;/script&gt;")]
    [InlineData("Line1\r\nLine2", "Line1 Line2")]
    [InlineData(null, "—")]
    [InlineData("", "—")]
    public void SanitizeMarkdownCell_SanitizesPipesAndTags(string? input, string expected)
    {
        string actual = AppUtils.SanitizeMarkdownCell(input);
        Assert.Equal(expected, actual);
    }
}
