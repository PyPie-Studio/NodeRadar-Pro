namespace NodeRadarPro.Core.Tests
{
    public class AppUtilsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-url")]
        [InlineData("ftp://example.com")]
        [InlineData("file:///C:/Windows/System32/calc.exe")]
        [InlineData("javascript:alert(1)")]
        public void OpenSafeUrl_InvalidOrNonHttpUrl_DoesNotThrow(string? invalidUrl)
        {
            var exception = Record.Exception(() => AppUtils.OpenSafeUrl(invalidUrl!));
            Assert.Null(exception);
        }

        [Fact]
        public void OpenSafeUrl_ValidHttpUrl_DoesNotThrow()
        {
            var exception = Record.Exception(() => AppUtils.OpenSafeUrl("https://example.com"));
            Assert.Null(exception);
        }
    }
}
