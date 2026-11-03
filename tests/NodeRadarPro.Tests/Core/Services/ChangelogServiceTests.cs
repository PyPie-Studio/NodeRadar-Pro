namespace NodeRadarPro.Tests.Core.Services;

public class ChangelogServiceTests
{
    private const string KeepAChangelogMarkdown = """
        # NodeRadar Pro Changelog

        All notable changes to this project will be documented in this file.

        The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
        and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

        ## [Unreleased]

        ### Added
        - In-flight work item for upcoming version.

        ## [2.0.3] - 2026-09-27

        ### Fixed
        - **Avalonia Resource URI Assembly Alignment**: Fixed desktop startup crash.
        - **Defensive UI Asset Loading**: Wrapped window icon and navigation logo loading.

        ### Changed
        - Centralized icon loading routines.

        ## [2.0.2] - 2026-09-27

        ### Changed
        - **Zero-Allocation Linux ARP Parsing**: Replaced whole-file ReadAllText.
        - **Zero-Allocation Subnet IP Parsing**: Replaced IP string splitting with stack spans.

        ## [1.0.0] - 2026-08-15

        ### Added
        - Initial release of NodeRadar Pro network reconnaissance suite.

        [Unreleased]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v2.0.3...HEAD
        [2.0.3]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v2.0.2...v2.0.3
        [2.0.2]: https://github.com/PyPie-Studio/NodeRadar-Pro/compare/v1.0.0...v2.0.2
        [1.0.0]: https://github.com/PyPie-Studio/NodeRadar-Pro/releases/tag/v1.0.0
        """;

    private const string LegacyMarkdown = """
        # NodeRadar Pro Changelog

        ## NodeRadar Pro v2.0.3 (2026-09-27)

        - **Avalonia Resource URI Assembly Alignment**: Fixed desktop startup crash.
        - **Defensive UI Asset Loading**: Wrapped window icon and navigation logo loading.
        """;

    [Fact]
    public void ParseChangelog_ParsesKeepAChangelogStructureAndCategories()
    {
        var releases = ChangelogService.ParseChangelog(KeepAChangelogMarkdown);

        Assert.Equal(3, releases.Count);

        // Version 2.0.3
        Assert.Equal("2.0.3", releases[0].Version);
        Assert.Equal("2026-09-27", releases[0].Date);
        Assert.Equal(3, releases[0].Highlights.Count);
        Assert.Equal("Avalonia Resource URI Assembly Alignment: Fixed desktop startup crash.", releases[0].Highlights[0]);
        Assert.Equal(3, releases[0].Items.Count);
        Assert.Equal("Fixed", releases[0].Items[0].Category);
        Assert.Equal("Avalonia Resource URI Assembly Alignment: Fixed desktop startup crash.", releases[0].Items[0].Text);
        Assert.Equal("Fixed", releases[0].Items[1].Category);
        Assert.Equal("Changed", releases[0].Items[2].Category);

        // Version 2.0.2
        Assert.Equal("2.0.2", releases[1].Version);
        Assert.Equal("2026-09-27", releases[1].Date);
        Assert.Equal(2, releases[1].Highlights.Count);
        Assert.Equal(2, releases[1].Items.Count);
        Assert.Equal("Changed", releases[1].Items[0].Category);

        // Version 1.0.0
        Assert.Equal("1.0.0", releases[2].Version);
        Assert.Equal("2026-08-15", releases[2].Date);
        Assert.Single(releases[2].Highlights);
        Assert.Single(releases[2].Items);
        Assert.Equal("Added", releases[2].Items[0].Category);
    }

    [Fact]
    public void ParseChangelog_ParsesLegacyMarkdownFormats()
    {
        var releases = ChangelogService.ParseChangelog(LegacyMarkdown);

        Assert.Single(releases);
        Assert.Equal("2.0.3", releases[0].Version);
        Assert.Equal("2026-09-27", releases[0].Date);
        Assert.Equal(2, releases[0].Highlights.Count);
        Assert.Equal("Changed", releases[0].Items[0].Category);
    }

    [Fact]
    public void GetRecentReleases_ReturnsValidReleasesFromEmbeddedOrFallback()
    {
        var releases = ChangelogService.GetRecentReleases(3);

        Assert.NotEmpty(releases);
        Assert.True(releases.Count <= 3);
        Assert.Equal("2.2.0", releases[0].Version);
        Assert.NotEmpty(releases[0].Highlights);
        Assert.NotEmpty(releases[0].Items);
        Assert.True(releases[0].Items.Count >= releases[0].Highlights.Count);
    }

    [Fact]
    public void GetFallbackReleases_ReturnsStructuredFallbackEntries()
    {
        var fallbacks = ChangelogService.GetFallbackReleases();

        Assert.NotEmpty(fallbacks);
        Assert.Equal("2.0.3", fallbacks[0].Version);
        Assert.Equal("2026-09-27", fallbacks[0].Date);
        Assert.NotEmpty(fallbacks[0].Highlights);
        Assert.NotEmpty(fallbacks[0].Items);
        Assert.True(fallbacks.Count >= 5);
    }

    [Fact]
    public void ChangelogRelease_BackwardCompatibleConstructor_PopulatesItemsWithDefaultCategory()
    {
        var release = new ChangelogRelease("1.0.0", "2026-08-15", ["Sample highlight"]);

        Assert.Equal("1.0.0", release.Version);
        Assert.Equal("2026-08-15", release.Date);
        Assert.Single(release.Highlights);
        Assert.Single(release.Items);
        Assert.Equal("Changed", release.Items[0].Category);
        Assert.Equal("Sample highlight", release.Items[0].Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \r\n   ")]
    public void ParseChangelog_EmptyOrWhitespace_ReturnsEmptyList(string markdown)
    {
        var releases = ChangelogService.ParseChangelog(markdown);
        Assert.Empty(releases);
    }
}
