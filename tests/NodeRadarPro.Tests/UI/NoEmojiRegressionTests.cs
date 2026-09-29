using System.Text.RegularExpressions;

namespace NodeRadarPro.Tests.UI;

/// <summary>
/// Regression test suite ensuring that emojis are never used in UI or Core code,
/// enforcing the architectural standard that vector SVGs (ThemeTokens.VectorIcon)
/// are used for all technical icons.
/// </summary>
public partial class NoEmojiRegressionTests
{
    private static readonly Regex EmojiRegex = MyEmojiRegex();

    [Fact]
    public void ProductionCode_ContainsZeroEmojis()
    {
        // Locate src directory
        string baseDir = AppContext.BaseDirectory;
        string? srcDir = null;

        var probe = new DirectoryInfo(baseDir);
        while (probe != null)
        {
            string candidate = Path.Combine(probe.FullName, "src", "NodeRadarPro");
            if (Directory.Exists(candidate))
            {
                srcDir = candidate;
                break;
            }
            probe = probe.Parent;
        }

        Assert.NotNull(srcDir);
        Assert.True(Directory.Exists(srcDir), $"Source directory not found: {srcDir}");

        var violations = new List<string>();

        var files = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            // Skip obj/ and bin/ directories
            if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) ||
                file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                var match = EmojiRegex.Match(line);
                if (match.Success)
                {
                    string relativePath = Path.GetRelativePath(srcDir, file);
                    violations.Add($"{relativePath}:{i + 1}: Found emoji '{match.Value}' in line: {line.Trim()}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} emoji regression(s) in production source code. Use ThemeTokens.VectorIcon instead:\n" +
            string.Join("\n", violations));
    }

    [GeneratedRegex(@"[\uD83C-\uDBFF][\uDC00-\uDFFF]|[\u26A0\u26A1\u2705\u274C\u274E\u2728\u2139\u2714\u2716\u2702-\u27B0\u231A-\u23FA]")]
    private static partial Regex MyEmojiRegex();
}
