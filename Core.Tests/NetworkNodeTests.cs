using NodeRadarPro.Core;

namespace Core.Tests;

public class NetworkNodeTests
{
    [Fact]
    public void PacketLossPct_InitialState_ReturnsZero()
    {
        // Arrange
        var node = new NetworkNode();

        // Act & Assert
        Assert.Empty(node.PingHistory);
        Assert.Equal(0.0, node.PacketLossPct);
    }

    [Fact]
    public void RecordPing_LessThan100Pings_UpdatesHistoryAndCalculatesCorrectLoss()
    {
        // Arrange
        var node = new NetworkNode();

        // Act
        node.RecordPing(true);
        node.RecordPing(false);
        node.RecordPing(true);
        node.RecordPing(true);

        // Assert
        Assert.Equal(4, node.PingHistory.Count);
        // 1 failure out of 4 = 25% packet loss
        Assert.Equal(25.0, node.PacketLossPct);
    }

    [Fact]
    public void RecordPing_Exactly100Pings_DoesNotDequeue()
    {
        // Arrange
        var node = new NetworkNode();

        // Act
        for (int i = 0; i < 100; i++)
        {
            node.RecordPing(i % 2 == 0); // Alternate true/false, exactly 50 failures
        }

        // Assert
        Assert.Equal(100, node.PingHistory.Count);
        Assert.Equal(50.0, node.PacketLossPct);
    }

    [Fact]
    public void RecordPing_MoreThan100Pings_DequeuesOldestResults()
    {
        // Arrange
        var node = new NetworkNode();

        // Fill queue with 100 successful pings (0% loss)
        for (int i = 0; i < 100; i++)
        {
            node.RecordPing(true);
        }

        Assert.Equal(100, node.PingHistory.Count);
        Assert.Equal(0.0, node.PacketLossPct);

        // Act
        // Add 10 failed pings. This should push out 10 successful pings.
        // Resulting queue: 90 successful, 10 failed. (10% loss)
        for (int i = 0; i < 10; i++)
        {
            node.RecordPing(false);
        }

        // Assert
        Assert.Equal(100, node.PingHistory.Count);
        Assert.Equal(10.0, node.PacketLossPct);
    }

    [Fact]
    public void RecordPing_101stPing_EvictsOldestEntry()
    {
        // Arrange
        var node = new NetworkNode();
        node.RecordPing(false); // 1st ping is a failure

        for (int i = 0; i < 99; i++)
        {
            node.RecordPing(true);
        }

        Assert.Equal(100, node.PingHistory.Count);
        Assert.Equal(1.0, node.PacketLossPct);

        // Act: 101st ping (successful) should evict the 1st failed ping
        node.RecordPing(true);

        // Assert
        Assert.Equal(100, node.PingHistory.Count);
        Assert.Equal(0.0, node.PacketLossPct);
    }

    [Fact]
    public void RecordPing_FullBufferRollover_CompletelyReplacesHistory()
    {
        // Arrange
        var node = new NetworkNode();
        for (int i = 0; i < 100; i++)
        {
            node.RecordPing(false);
        }
        Assert.Equal(100.0, node.PacketLossPct);

        // Act: push 100 successful pings to completely overwrite history
        for (int i = 0; i < 100; i++)
        {
            node.RecordPing(true);
        }

        // Assert
        Assert.Equal(100, node.PingHistory.Count);
        Assert.Equal(0.0, node.PacketLossPct);
    }

    [Fact]
    public void RecordPing_SinglePingEdgeCases_CalculatesCorrectLoss()
    {
        var node1 = new NetworkNode();
        node1.RecordPing(true);
        Assert.Single(node1.PingHistory);
        Assert.Equal(0.0, node1.PacketLossPct);

        var node2 = new NetworkNode();
        node2.RecordPing(false);
        Assert.Single(node2.PingHistory);
        Assert.Equal(100.0, node2.PacketLossPct);
    }

    [Fact]
    public void RecordPing_LargeVolumePings_Maintains100ItemLimit()
    {
        // Arrange
        var node = new NetworkNode();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            node.RecordPing(i % 10 == 0); // 1 success out of 10
        }

        // Assert
        Assert.Equal(100, node.PingHistory.Count);
        // Out of the last 100 entries (indices 900 to 999), 10 are successful (i % 10 == 0) and 90 are failures.
        // 90 failures / 100 = 90.0% loss.
        Assert.Equal(90.0, node.PacketLossPct);
    }
}
