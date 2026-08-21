using Avalonia.Controls.Notifications;
using NodeRadarPro.Core;
using Moq;

namespace NodeRadarPro.Tests
{
    public class IntrusionAlerterTests
    {
        [Fact]
        public void AlertDeviceOffline_WhenDisabled_DoesNotThrow()
        {
            var node = new NetworkNode { CustomName = "Test Node", IpAddress = "192.168.1.100", MacAddress = "00:11:22:33:44:55" };
            IntrusionAlerter.Enabled = false;

            var exception = Record.Exception(() => IntrusionAlerter.AlertDeviceOffline(node));

            Assert.Null(exception);
        }

        [Fact]
        public void AlertDeviceOffline_WhenEnabled_NoNotificationManager_DoesNotThrow()
        {
            var node = new NetworkNode { CustomName = "Test Node", IpAddress = "192.168.1.100", MacAddress = "00:11:22:33:44:55" };
            IntrusionAlerter.Enabled = true;
            IntrusionAlerter.SetNotificationManagerForTesting(null);

            var exception = Record.Exception(() => IntrusionAlerter.AlertDeviceOffline(node));

            Assert.Null(exception);
        }

        [Fact]
        public void AlertDeviceOffline_WhenEnabledAndNotificationManagerExists_ShowsNotification()
        {
            // Arrange
            var node = new NetworkNode { CustomName = "Test Node", IpAddress = "192.168.1.100", MacAddress = "00:11:22:33:44:55" };
            IntrusionAlerter.Enabled = true;

            var mockNotificationManager = new Mock<INotificationManager>();
            IntrusionAlerter.SetNotificationManagerForTesting(mockNotificationManager.Object);

            var originalAudioEnabled = AudioService.Enabled;
            AudioService.Enabled = false; // Disable audio to prevent exceptions during test

            try
            {
                // Act
                IntrusionAlerter.AlertDeviceOffline(node);

                // Assert
                mockNotificationManager.Verify(m => m.Show(It.Is<INotification>(n =>
                    n.Title == "⚠️ Device Disconnected" &&
                    n.Type == NotificationType.Error)),
                    Times.Once);
            }
            finally
            {
                // Cleanup
                IntrusionAlerter.SetNotificationManagerForTesting(null);
                AudioService.Enabled = originalAudioEnabled;
            }
        }

        [Fact]
        public void AlertDeviceReconnected_WhenEnabledAndNotificationManagerExists_ShowsNotification()
        {
            // Arrange
            var node = new NetworkNode { CustomName = "Test Node", IpAddress = "192.168.1.100", MacAddress = "00:11:22:33:44:55" };
            IntrusionAlerter.Enabled = true;

            var mockNotificationManager = new Mock<INotificationManager>();
            IntrusionAlerter.SetNotificationManagerForTesting(mockNotificationManager.Object);

            var originalAudioEnabled = AudioService.Enabled;
            AudioService.Enabled = false; // Disable audio to prevent exceptions during test

            try
            {
                // Act
                IntrusionAlerter.AlertDeviceReconnected(node);

                // Assert
                mockNotificationManager.Verify(m => m.Show(It.Is<INotification>(n =>
                    n.Title == "✅ Device Reconnected" &&
                    n.Type == NotificationType.Success)),
                    Times.Once);
            }
            finally
            {
                // Cleanup
                IntrusionAlerter.SetNotificationManagerForTesting(null);
                AudioService.Enabled = originalAudioEnabled;
            }
        }

        [Fact]
        public void AlertNewDevice_WhenEnabledAndNotificationManagerExists_ShowsNotification()
        {
            // Arrange
            var node = new NetworkNode { CustomName = "Test Node", IpAddress = "192.168.1.100", MacAddress = "00:11:22:33:44:55" };
            IntrusionAlerter.Enabled = true;

            var mockNotificationManager = new Mock<INotificationManager>();
            IntrusionAlerter.SetNotificationManagerForTesting(mockNotificationManager.Object);

            var originalAudioEnabled = AudioService.Enabled;
            AudioService.Enabled = false; // Disable audio to prevent exceptions during test

            try
            {
                // Act
                IntrusionAlerter.AlertNewDevice(node);

                // Assert
                mockNotificationManager.Verify(m => m.Show(It.Is<INotification>(n =>
                    n.Title == "🔵 New Device Discovered" &&
                    n.Type == NotificationType.Information)),
                    Times.Once);
            }
            finally
            {
                // Cleanup
                IntrusionAlerter.SetNotificationManagerForTesting(null);
                AudioService.Enabled = originalAudioEnabled;
            }
        }
    }
}
