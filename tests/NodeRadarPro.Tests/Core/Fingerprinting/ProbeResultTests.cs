using NodeRadarPro.Core.Fingerprinting;

namespace NodeRadarPro.Tests.Fingerprinting;

public class ProbeResultTests
{
    [Fact]
    public void GetValue_ExistingKey_ReturnsValue()
    {
        // Arrange
        var result = new ProbeResult();
        result.RawData.Add("MAC", "00:11:22:33:44:55");

        // Act
        var value = result.GetValue("MAC");

        // Assert
        Assert.Equal("00:11:22:33:44:55", value);
    }

    [Fact]
    public void GetValue_MissingKey_ReturnsNull()
    {
        // Arrange
        var result = new ProbeResult();

        // Act
        var value = result.GetValue("NonExistentKey");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetValue_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var result = new ProbeResult();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => result.GetValue(null!));
    }
}
