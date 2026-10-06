using NodeRadarPro.Core;

namespace NodeRadarPro.Tests.Core;

public class ChangelogServiceTests
{
    private const string SampleMarkdown = """
        # NodeRadar Pro Changelog

        ## NodeRadar Pro v2.0.3 (2026-09-27)

        ### Bug Fixes & Application Resilience
        - **Avalonia Resource URI Assembly Alignment**: Fixed desktop startup crash.
        - **Defensive UI Asset Loading**: Wrapped window icon and navigation logo loading.

        ## NodeRadar Pro v2.0.2 (2026-09-27)

        ### Performance and Zero-Allocation Networking
        - **Zero-Allocation Linux ARP Parsing**: Replaced whole-file ReadAllText.
        - **Zero-Allocation Subnet IP Parsing**: Replaced IP string splitting with stack spans.

        ## NodeRadar Pro v1.0.0 (2026-08-15)

        - Initial release of NodeRadar Pro network reconnaissance suite.
        """;

    [Fact]
    public void ParseChangelog_ParsesVersionsDatesAndHighlights()
    {
        var releases = ChangelogService.ParseChangelog(SampleMarkdown);

        Assert.Equal(3, releases.Count);

        Assert.Equal("2.0.3", releases[0].Version);
        Assert.Equal("2026-09-27", releases[0].Date);
        Assert.Equal(2, releases[0].Highlights.Count);
        Assert.Equal("Avalonia Resource URI Assembly Alignment: Fixed desktop startup crash.", releases[0].Highlights[0]);

        Assert.Equal("2.0.2", releases[1].Version);
        Assert.Equal("2026-09-27", releases[1].Date);
        Assert.Equal(2, releases[1].Highlights.Count);

        Assert.Equal("1.0.0", releases[2].Version);
        Assert.Equal("2026-08-15", releases[2].Date);
        Assert.Single(releases[2].Highlights);
    }

    [Fact]
    public void GetRecentReleases_ReturnsValidReleasesFromEmbeddedOrFallback()
    {
        var releases = ChangelogService.GetRecentReleases(3);

        Assert.NotEmpty(releases);
        Assert.True(releases.Count <= 3);
        Assert.Equal("2.0.8", releases[0].Version);
        Assert.NotEmpty(releases[0].Highlights);
    }
}
