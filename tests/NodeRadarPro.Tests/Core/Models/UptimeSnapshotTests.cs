using LiteDB;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests.Core.Models;

public class UptimeSnapshotTests
{
    [Fact]
    public void Constructor_DefaultValues_InitializedCorrectly()
    {
        // Arrange & Act
        var before = DateTime.UtcNow.AddSeconds(-1);
        var snapshot = new UptimeSnapshot();
        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.NotEqual(ObjectId.Empty, snapshot.Id);
        Assert.Equal(string.Empty, snapshot.MacAddress);
        Assert.False(snapshot.IsOnline);
        Assert.Equal(-1, snapshot.LatencyMs);
        Assert.True(snapshot.Timestamp >= before && snapshot.Timestamp <= after, $"Timestamp {snapshot.Timestamp} was not between {before} and {after}");
    }

    [Fact]
    public void Properties_SetAndGet_ReturnsExpectedValues()
    {
        // Arrange
        var customId = ObjectId.NewObjectId();
        var customTimestamp = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var snapshot = new UptimeSnapshot();

        // Act
        snapshot.Id = customId;
        snapshot.MacAddress = "AA:BB:CC:DD:EE:FF";
        snapshot.IsOnline = true;
        snapshot.LatencyMs = 15;
        snapshot.Timestamp = customTimestamp;

        // Assert
        Assert.Equal(customId, snapshot.Id);
        Assert.Equal("AA:BB:CC:DD:EE:FF", snapshot.MacAddress);
        Assert.True(snapshot.IsOnline);
        Assert.Equal(15, snapshot.LatencyMs);
        Assert.Equal(customTimestamp, snapshot.Timestamp);
    }

    [Fact]
    public void ObjectId_NewInstances_HaveUniqueIds()
    {
        // Arrange & Act
        var snapshot1 = new UptimeSnapshot();
        var snapshot2 = new UptimeSnapshot();

        // Assert
        Assert.NotEqual(snapshot1.Id, snapshot2.Id);
    }
}
