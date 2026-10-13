using LiteDB;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests.Core.Models;

public class LogEntryTests
{
    [Fact]
    public void Constructor_DefaultValues_InitializedCorrectly()
    {
        // Arrange & Act
        var beforeUtc = DateTime.UtcNow;
        var entry = new LogEntry();
        var afterUtc = DateTime.UtcNow;

        // Assert
        Assert.NotEqual(ObjectId.Empty, entry.Id);
        Assert.InRange(entry.Timestamp, beforeUtc, afterUtc);
        Assert.Equal(LogLevel.Info, entry.Level);
        Assert.Equal(string.Empty, entry.Source);
        Assert.Equal(string.Empty, entry.Message);
        Assert.Null(entry.DeviceMac);
    }

    [Fact]
    public void Properties_SetAndGetCustomValues_ReturnsExpectedValues()
    {
        // Arrange
        var customId = ObjectId.NewObjectId();
        var customTimestamp = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var customLevel = LogLevel.Warning;
        var customSource = "NetworkScanner";
        var customMessage = "High latency detected";
        var customMac = "00:11:22:33:44:55";

        // Act
        var entry = new LogEntry
        {
            Id = customId,
            Timestamp = customTimestamp,
            Level = customLevel,
            Source = customSource,
            Message = customMessage,
            DeviceMac = customMac
        };

        // Assert
        Assert.Equal(customId, entry.Id);
        Assert.Equal(customTimestamp, entry.Timestamp);
        Assert.Equal(customLevel, entry.Level);
        Assert.Equal(customSource, entry.Source);
        Assert.Equal(customMessage, entry.Message);
        Assert.Equal(customMac, entry.DeviceMac);
    }

    [Fact]
    public void LogLevel_EnumValues_ContainExpectedDefinedValues()
    {
        // Assert
        Assert.Equal(0, (int)LogLevel.Info);
        Assert.Equal(1, (int)LogLevel.Warning);
        Assert.Equal(2, (int)LogLevel.Error);
        Assert.True(Enum.IsDefined(LogLevel.Info));
        Assert.True(Enum.IsDefined(LogLevel.Warning));
        Assert.True(Enum.IsDefined(LogLevel.Error));
    }
}
