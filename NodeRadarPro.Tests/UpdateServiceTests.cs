using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Security;
using Xunit;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    public MockHttpMessageHandler(string response, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseFactory = (request) => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(response)
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_responseFactory(request));
    }

}

public class UpdateServiceTests
{
    [Fact]
    public async Task CheckForUpdatesAsync_ValidNewVersion_ReturnsTrueAndDetails()
    {
        var jsonResponse = """
        {
            "tag_name": "v1.2.0",
            "assets": [
                {
                    "name": "NodeRadarPro_1.2.0.exe",
                    "browser_download_url": "https://github.com/pypiestudio/noderadar-pro/releases/download/v1.2.0/NodeRadarPro_1.2.0.exe"
                }
            ]
        }
        """;
        var handler = new MockHttpMessageHandler(jsonResponse);
        using var client = new HttpClient(handler);

        var result = await UpdateService.CheckForUpdatesAsync("1.0.0", client);

        Assert.True(result.hasUpdate);
        Assert.Equal("1.2.0", result.version);
        Assert.Equal("https://github.com/pypiestudio/noderadar-pro/releases/download/v1.2.0/NodeRadarPro_1.2.0.exe", result.downloadUrl);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_SameVersion_ReturnsFalse()
    {
        var jsonResponse = """
        {
            "tag_name": "v1.0.0",
            "assets": [
                {
                    "name": "NodeRadarPro_1.0.0.exe",
                    "browser_download_url": "https://github.com/pypiestudio/noderadar-pro/releases/download/v1.0.0/NodeRadarPro_1.0.0.exe"
                }
            ]
        }
        """;
        var handler = new MockHttpMessageHandler(jsonResponse);
        using var client = new HttpClient(handler);

        var result = await UpdateService.CheckForUpdatesAsync("1.0.0", client);

        Assert.False(result.hasUpdate);
        Assert.Equal("", result.version);
        Assert.Equal("", result.downloadUrl);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_MissingTagName_ReturnsFalse()
    {
        var jsonResponse = """
        {
            "assets": [
                {
                    "name": "NodeRadarPro_1.2.0.exe",
                    "browser_download_url": "https://github.com/pypiestudio/noderadar-pro/releases/download/v1.2.0/NodeRadarPro_1.2.0.exe"
                }
            ]
        }
        """;
        var handler = new MockHttpMessageHandler(jsonResponse);
        using var client = new HttpClient(handler);

        var result = await UpdateService.CheckForUpdatesAsync("1.0.0", client);

        Assert.False(result.hasUpdate);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_MissingExeAsset_ReturnsFalse()
    {
        var jsonResponse = """
        {
            "tag_name": "v1.2.0",
            "assets": [
                {
                    "name": "NodeRadarPro_1.2.0.zip",
                    "browser_download_url": "https://github.com/pypiestudio/noderadar-pro/releases/download/v1.2.0/NodeRadarPro_1.2.0.zip"
                }
            ]
        }
        """;
        var handler = new MockHttpMessageHandler(jsonResponse);
        using var client = new HttpClient(handler);

        var result = await UpdateService.CheckForUpdatesAsync("1.0.0", client);

        Assert.False(result.hasUpdate);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_HttpError_ReturnsFalse()
    {
        var handler = new MockHttpMessageHandler((req) => throw new HttpRequestException("Network error"));
        using var client = new HttpClient(handler);

        var result = await UpdateService.CheckForUpdatesAsync("1.0.0", client);

        Assert.False(result.hasUpdate);
    }


    [Fact]
    public async Task DownloadAndInstallAsync_UntrustedUrl_ThrowsSecurityException()
    {
        var untrustedUrl = "https://malicious-site.com/payload.exe";

        var ex = await Assert.ThrowsAsync<SecurityException>(() =>
            UpdateService.DownloadAndInstallAsync(untrustedUrl));

        Assert.Equal("Invalid or untrusted download URL.", ex.Message);
    }

}
