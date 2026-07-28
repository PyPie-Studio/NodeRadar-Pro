using System;
using System.Threading.Tasks;
using Xunit;
using NodeRadarPro.Core;

namespace Core.Tests
{
    public class WakeOnLanTests
    {
        [Fact]
        public async Task WakeAsync_WithValidMacAddressColons_ReturnsTrue()
        {
            string validMac = "AA:BB:CC:DD:EE:FF";
            bool result = await WakeOnLan.WakeAsync(validMac);
            Assert.True(result);
        }

        [Fact]
        public async Task WakeAsync_WithValidMacAddressDashes_ReturnsTrue()
        {
            string validMac = "AA-BB-CC-DD-EE-FF";
            bool result = await WakeOnLan.WakeAsync(validMac);
            Assert.True(result);
        }

        [Fact]
        public async Task WakeAsync_WithValidMacAddressPeriods_ReturnsTrue()
        {
            string validMac = "AA.BB.CC.DD.EE.FF";
            bool result = await WakeOnLan.WakeAsync(validMac);
            Assert.True(result);
        }

        [Fact]
        public async Task WakeAsync_WithValidMacAddressNoSeparators_ReturnsTrue()
        {
            string validMac = "AABBCCDDEEFF";
            bool result = await WakeOnLan.WakeAsync(validMac);
            Assert.True(result);
        }

        [Fact]
        public async Task WakeAsync_WithInvalidMacAddressTooShort_ReturnsFalse()
        {
            string invalidMac = "AA:BB:CC"; // Too short
            bool result = await WakeOnLan.WakeAsync(invalidMac);
            Assert.False(result);
        }

        [Fact]
        public async Task WakeAsync_WithInvalidMacAddressTooLong_ReturnsFalse()
        {
            string invalidMac = "AA:BB:CC:DD:EE:FF:11:22"; // Too long
            bool result = await WakeOnLan.WakeAsync(invalidMac);
            Assert.False(result);
        }

        [Fact]
        public async Task WakeAsync_WithInvalidCharacters_ReturnsFalse()
        {
            string invalidMac = "XYZABCMN1234"; // Not hex

            bool result = await WakeOnLan.WakeAsync(invalidMac);

            Assert.False(result);
        }
    }
}
