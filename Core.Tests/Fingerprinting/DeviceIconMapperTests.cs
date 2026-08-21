using NodeRadarPro.Core.Fingerprinting;

namespace Core.Tests.Fingerprinting;

public class DeviceIconMapperTests
{
    [Theory]
    [InlineData("Apple", "my-iphone", "", "phone")]
    [InlineData("apple inc.", "", "iphone 12", "phone")]
    [InlineData("APPLE", "Jane-iPad", "", "tablet")]
    [InlineData("Apple", "", "IPAD pro", "tablet")]
    [InlineData("Apple", "MacBook-Pro", "", "pc")]
    [InlineData("apple", "imac-27", "", "pc")]
    [InlineData("Apple", "mac-mini", "", "pc")]
    [InlineData("Apple", "unknown-device", "", "phone")] // Default Apple device
    public void GetIconKey_AppleOverrides_ReturnsCorrectIcon(string vendor, string hostname, string combinedData, string expectedIcon)
    {
        // Arrange
        var category = DeviceTypeCategory.Unknown;

        // Act
        var result = DeviceIconMapper.GetIconKey(category, vendor, hostname, combinedData);

        // Assert
        Assert.Equal(expectedIcon, result);
    }

    [Theory]
    [InlineData(DeviceTypeCategory.Mobile, "phone")]
    [InlineData(DeviceTypeCategory.Desktop, "pc")]
    [InlineData(DeviceTypeCategory.Server, "server")]
    [InlineData(DeviceTypeCategory.NAS, "nas")]
    [InlineData(DeviceTypeCategory.Router, "router")]
    [InlineData(DeviceTypeCategory.Switch, "switch")]
    [InlineData(DeviceTypeCategory.Firewall, "firewall")]
    [InlineData(DeviceTypeCategory.AccessPoint, "accesspoint")]
    [InlineData(DeviceTypeCategory.Printer, "printer")]
    [InlineData(DeviceTypeCategory.TV, "tv")]
    [InlineData(DeviceTypeCategory.Speaker, "speaker")]
    [InlineData(DeviceTypeCategory.Camera, "camera")]
    [InlineData(DeviceTypeCategory.DVR, "dvr")]
    [InlineData(DeviceTypeCategory.GameConsole, "gamepad")]
    [InlineData(DeviceTypeCategory.IoT, "iot")]
    [InlineData(DeviceTypeCategory.Unknown, "pc")] // Default fallback
    public void GetIconKey_CategoryMapping_ReturnsCorrectIcon(DeviceTypeCategory type, string expectedIcon)
    {
        // Arrange
        var vendor = "Generic";
        var hostname = "device";
        var combinedData = "";

        // Act
        var result = DeviceIconMapper.GetIconKey(type, vendor, hostname, combinedData);

        // Assert
        Assert.Equal(expectedIcon, result);
    }
}
