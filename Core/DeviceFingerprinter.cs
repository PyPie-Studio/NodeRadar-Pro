using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core;

public static class DeviceFingerprinter
{
    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

    public static async Task<string> TryGetHttpServerBannerAsync(string ip)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"http://{ip}/");
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.Headers.Server != null)
            {
                return response.Headers.Server.ToString();
            }
        }
        catch { }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://{ip}/");
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.Headers.Server != null)
            {
                return response.Headers.Server.ToString();
            }
        }
        catch { }

        return string.Empty;
    }
}
