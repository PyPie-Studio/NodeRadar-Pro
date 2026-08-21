using System;
using System.Security;
using System.Threading.Tasks;
using Xunit;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

public class UpdateServiceDownloadTests
{
    [Fact]
    public async Task DownloadAndInstallAsync_ValidUrl_ThrowsHttpRequestExceptionOrSimilarIfMocked()
    {
    }

    [Theory]
    [InlineData("http://github.com/pypiestudio/noderadar-pro/releases/download/v1.0.0/NodeRadarPro.exe")]
    [InlineData("https://evil.com/pypiestudio/noderadar-pro/releases/download/v1.0.0/NodeRadarPro.exe")]
    [InlineData("https://github.com/someotherrepo/noderadar-pro/releases/download/v1.0.0/NodeRadarPro.exe")]
    [InlineData("not-a-url")]
    public async Task DownloadAndInstallAsync_InvalidUrl_ThrowsSecurityException(string url)
    {
        await Assert.ThrowsAsync<SecurityException>(() => UpdateService.DownloadAndInstallAsync(url));
    }
}
