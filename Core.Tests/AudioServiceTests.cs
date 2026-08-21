using System;
using Xunit;
using NodeRadarPro.Core;

namespace Core.Tests
{
    public class AudioServiceTests : IDisposable
    {
        public AudioServiceTests()
        {
            AudioService.Enabled = true;
        }

        public void Dispose()
        {
            AudioService.Enabled = true;
        }

        [Fact]
        public void Enabled_CanBeSetAndRead()
        {
            AudioService.Enabled = false;
            Assert.False(AudioService.Enabled);

            AudioService.Enabled = true;
            Assert.True(AudioService.Enabled);
        }

        [Fact]
        public void PlayAlert_WhenDisabled_DoesNotThrow()
        {
            AudioService.Enabled = false;
            var exception = Record.Exception(() => AudioService.PlayAlert(true));
            Assert.Null(exception);

            exception = Record.Exception(() => AudioService.PlayAlert(false));
            Assert.Null(exception);
        }

        [Fact]
        public void PlayAlert_WhenCriticalIsTrue_ExecutesWithoutException()
        {
            AudioService.Enabled = true;
            var exception = Record.Exception(() => AudioService.PlayAlert(true));
            Assert.Null(exception);
        }

        [Fact]
        public void PlayAlert_WhenCriticalIsFalse_ExecutesWithoutException()
        {
            AudioService.Enabled = true;
            var exception = Record.Exception(() => AudioService.PlayAlert(false));
            Assert.Null(exception);
        }
    }
}
