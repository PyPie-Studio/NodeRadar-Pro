using System;
using System.Threading.Tasks;
using Xunit;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests
{
    public class WakeOnLanTests
    {
        [Fact]
        public async Task WakeAsync_NullMacAddress_ReturnsFalse()
        {
            // Act
            bool result = await WakeOnLan.WakeAsync(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task WakeAsync_EmptyMacAddress_ReturnsFalse()
        {
            // Act
            bool result = await WakeOnLan.WakeAsync(string.Empty);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("AA:BB:CC:DD:EE:F")] // 11 chars
        [InlineData("AA:BB:CC:DD:EE:FF:00")] // 14 chars
        public async Task WakeAsync_InvalidLengthMacAddress_ReturnsFalse(string macAddress)
        {
            // Act
            bool result = await WakeOnLan.WakeAsync(macAddress);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("ZZ:ZZ:ZZ:ZZ:ZZ:ZZ")]
        [InlineData("GG-GG-GG-GG-GG-GG")]
        [InlineData("00-00-00-00-00-XX")]
        public async Task WakeAsync_NonHexMacAddress_ReturnsFalse(string macAddress)
        {
            // Act
            bool result = await WakeOnLan.WakeAsync(macAddress);

            // Assert
            Assert.False(result);
        }
    }
}
