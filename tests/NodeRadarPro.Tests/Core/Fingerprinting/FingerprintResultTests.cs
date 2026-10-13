using NodeRadarPro.Core.Fingerprinting;

namespace NodeRadarPro.Tests.Fingerprinting;

public class FingerprintResultTests
{
    [Fact]
    public void FingerprintResult_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var result = new FingerprintResult();

        // Assert
        Assert.Equal("Unknown Vendor", result.Vendor);
        Assert.Equal("", result.Os);
        Assert.Equal("", result.Model);
        Assert.Equal(DeviceTypeCategory.Unknown, result.Type);
        Assert.Equal("Generic Device", result.TypeString);
        Assert.Equal("pc", result.IconSvgKey);
        Assert.Equal(0, result.ConfidenceScore);
    }

    [Fact]
    public void FingerprintResult_SettersAndGetters_WorkAsExpected()
    {
        // Arrange
        var result = new FingerprintResult
        {
            Vendor = "Cisco",
            Os = "IOS-XE",
            Model = "Catalyst 9300",
            Type = DeviceTypeCategory.Switch,
            TypeString = "Network Switch",
            IconSvgKey = "switch",
            ConfidenceScore = 85
        };

        // Assert
        Assert.Equal("Cisco", result.Vendor);
        Assert.Equal("IOS-XE", result.Os);
        Assert.Equal("Catalyst 9300", result.Model);
        Assert.Equal(DeviceTypeCategory.Switch, result.Type);
        Assert.Equal("Network Switch", result.TypeString);
        Assert.Equal("switch", result.IconSvgKey);
        Assert.Equal(85, result.ConfidenceScore);
    }

    [Theory]
    [InlineData(DeviceTypeCategory.Mobile)]
    [InlineData(DeviceTypeCategory.Desktop)]
    [InlineData(DeviceTypeCategory.Router)]
    [InlineData(DeviceTypeCategory.Switch)]
    [InlineData(DeviceTypeCategory.Firewall)]
    [InlineData(DeviceTypeCategory.AccessPoint)]
    [InlineData(DeviceTypeCategory.Printer)]
    [InlineData(DeviceTypeCategory.IoT)]
    [InlineData(DeviceTypeCategory.Camera)]
    [InlineData(DeviceTypeCategory.DVR)]
    [InlineData(DeviceTypeCategory.Server)]
    [InlineData(DeviceTypeCategory.NAS)]
    [InlineData(DeviceTypeCategory.TV)]
    [InlineData(DeviceTypeCategory.Speaker)]
    [InlineData(DeviceTypeCategory.GameConsole)]
    [InlineData(DeviceTypeCategory.Unknown)]
    public void DeviceTypeCategory_AllEnumValues_CanBeAssigned(DeviceTypeCategory category)
    {
        // Arrange
        var result = new FingerprintResult
        {
            Type = category
        };

        // Assert
        Assert.Equal(category, result.Type);
    }
}
