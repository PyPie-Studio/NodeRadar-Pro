using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NodeRadarPro.Core;

/// <summary>
/// Represents a structured release version entry parsed from CHANGELOG.md.
/// </summary>
public record ChangelogRelease(string Version, string Date, IReadOnlyList<string> Highlights);

/// <summary>
/// Loads and parses application release notes from embedded resources or local changelog files.
/// </summary>
public static partial class ChangelogService
{
    private static readonly Regex ReleaseHeaderRegex = MyReleaseHeaderRegex();
    private static readonly Regex BulletPointRegex = MyBulletPointRegex();

    private static IReadOnlyList<ChangelogRelease>? _cachedReleases;

    /// <summary>
    /// Retrieves recent application releases in reverse chronological order.
    /// </summary>
    public static IReadOnlyList<ChangelogRelease> GetRecentReleases(int maxCount = 5)
    {
        if (_cachedReleases != null)
        {
            return maxCount > 0 && _cachedReleases.Count > maxCount
                ? _cachedReleases.Take(maxCount).ToList()
                : _cachedReleases;
        }

        string? markdown = LoadChangelogMarkdown();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            _cachedReleases = GetFallbackReleases();
        }
        else
        {
            var parsed = ParseChangelog(markdown);
            _cachedReleases = parsed.Count > 0 ? parsed : GetFallbackReleases();
        }

        return maxCount > 0 && _cachedReleases.Count > maxCount
            ? _cachedReleases.Take(maxCount).ToList()
            : _cachedReleases;
    }

    /// <summary>
    /// Parses changelog markdown into structured release entries.
    /// </summary>
    public static List<ChangelogRelease> ParseChangelog(string markdown)
    {
        var releases = new List<ChangelogRelease>();
        using var reader = new StringReader(markdown);

        string? currentVersion = null;
        string? currentDate = null;
        var currentHighlights = new List<string>();

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var headerMatch = ReleaseHeaderRegex.Match(line);
            if (headerMatch.Success)
            {
                if (!string.IsNullOrEmpty(currentVersion))
                {
                    releases.Add(new ChangelogRelease(currentVersion, currentDate ?? "", currentHighlights.ToArray()));
                    currentHighlights.Clear();
                }

                currentVersion = headerMatch.Groups[1].Value;
                currentDate = headerMatch.Groups[2].Success ? headerMatch.Groups[2].Value : (headerMatch.Groups[3].Success ? headerMatch.Groups[3].Value : "");
                continue;
            }

            if (string.IsNullOrEmpty(currentVersion))
                continue;

            var bulletMatch = BulletPointRegex.Match(line);
            if (bulletMatch.Success)
            {
                string text = bulletMatch.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    currentHighlights.Add(CleanMarkdownText(text));
                }
            }
        }

        if (!string.IsNullOrEmpty(currentVersion))
        {
            releases.Add(new ChangelogRelease(currentVersion, currentDate ?? "", currentHighlights.ToArray()));
        }

        return releases;
    }

    private static string CleanMarkdownText(string text)
    {
        // Strip markdown bold **text** markers for clean UI presentation
        return text.Replace("**", "");
    }

    private static string? LoadChangelogMarkdown()
    {
        try
        {
            // 1. Try embedded resource
            var asm = typeof(ChangelogService).Assembly;
            using var stream = asm.GetManifestResourceStream("NodeRadarPro.Resources.CHANGELOG.md");
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }

            // 2. Try application base directory
            string localPath = Path.Combine(AppContext.BaseDirectory, "CHANGELOG.md");
            if (File.Exists(localPath))
            {
                return File.ReadAllText(localPath);
            }

            // 3. Try repo root relative to binary in development
            string devPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CHANGELOG.md");
            if (File.Exists(devPath))
            {
                return File.ReadAllText(devPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, "Changelog", $"Failed to load CHANGELOG.md: {ex.Message}");
        }

        return null;
    }

    private static List<ChangelogRelease> GetFallbackReleases() =>
    [
        new(
            "2.0.3",
            "2026-09-27",
            [
                "Avalonia resource URI assembly alignment ensuring zero startup asset crashes.",
                "Automated smoke verification step in local Master Quality Gate.",
                "Coverage ratchet tightened to 55% across all test suites."
            ]
        ),
        new(
            "2.0.2",
            "2026-09-27",
            [
                "Zero-allocation Linux ARP and subnet IP parsing with stack spans.",
                "Modularized security audit report generation with strict 12-hour AM/PM formatting.",
                "Automated rolling database backups with 7-generation retention."
            ]
        ),
        new(
            "2.0.1",
            "2026-09-20",
            [
                "Upfront administrative elevation in Inno Setup packaging.",
                "Frictionless upgrade workflows with automatic directory bypassing.",
                "Dynamic UI version telemetry linked to assembly metadata."
            ]
        ),
        new(
            "2.0.0",
            "2026-09-19",
            [
                "Open-source transition under permissive MIT license.",
                "Automated network environment pre-flight diagnostic scripts.",
                "Comprehensive architecture records and latency benchmark documentation."
            ]
        ),
        new(
            "1.0.0",
            "2026-08-15",
            [
                "Initial release: Subnet scanning, device inventory, port scanning, alert system and system logs."
            ]
        )
    ];

    [GeneratedRegex(@"^##\s+(?:NodeRadar\s+Pro\s+)?v?\[?([0-9]+\.[0-9]+\.[0-9]+)\]?(?:\s+\(([^)]+)\))?(?:\s*-\s*([0-9-]+))?")]
    private static partial Regex MyReleaseHeaderRegex();

    [GeneratedRegex(@"^[\s]*[-*]\s+(.*)$")]
    private static partial Regex MyBulletPointRegex();
}
