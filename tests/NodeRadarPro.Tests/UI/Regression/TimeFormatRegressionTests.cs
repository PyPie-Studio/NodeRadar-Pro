using System.Text.RegularExpressions;

namespace NodeRadarPro.Tests.UI.Regression;

/// <summary>
/// Architectural governance test suite enforcing the mandatory 12-hour AM/PM time format standard
/// and strict English LTR layout across all UI and production code.
/// </summary>
public partial class TimeFormatRegressionTests
{
    private static readonly Regex MilitaryTimeRegex = MyMilitaryTimeRegex();
    private static readonly Regex RightToLeftRegex = MyRightToLeftRegex();

    [GeneratedRegex(@"\bHH:mm(?::ss)?\b", RegexOptions.Compiled)]
    private static partial Regex MyMilitaryTimeRegex();

    [GeneratedRegex(@"(FlowDirection\.RightToLeft|FlowDirection\s*=\s*""RightToLeft"")", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex MyRightToLeftRegex();

    private static string GetSourceDirectory()
    {
        string baseDir = AppContext.BaseDirectory;
        var probe = new DirectoryInfo(baseDir);
        while (probe != null)
        {
            string candidate = Path.Combine(probe.FullName, "src", "NodeRadarPro");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            probe = probe.Parent;
        }

        throw new DirectoryNotFoundException($"Source directory 'src/NodeRadarPro' not found from {baseDir}");
    }

    [Fact]
    public void ProductionCode_ContainsZeroMilitaryTimeDisplayFormats()
    {
        string srcDir = GetSourceDirectory();
        var violations = new List<string>();

        var extensions = new[] { "*.cs", "*.axaml" };
        foreach (var ext in extensions)
        {
            var files = Directory.GetFiles(srcDir, ext, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) ||
                    file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                {
                    continue;
                }

                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    var match = MilitaryTimeRegex.Match(line);
                    if (match.Success)
                    {
                        string relativePath = Path.GetRelativePath(srcDir, file);
                        violations.Add($"{relativePath}:{i + 1}: Found 24-hour military time pattern '{match.Value}' in line: {line.Trim()}");
                    }
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} 24-hour military time violations (Mandatory 12-hour AM/PM 'hh:mm tt' required):\n" +
            string.Join("\n", violations));
    }

    [Fact]
    public void ProductionCode_ContainsZeroRightToLeftFlowDirection()
    {
        string srcDir = GetSourceDirectory();
        var violations = new List<string>();

        var extensions = new[] { "*.cs", "*.axaml" };
        foreach (var ext in extensions)
        {
            var files = Directory.GetFiles(srcDir, ext, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) ||
                    file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                {
                    continue;
                }

                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    var match = RightToLeftRegex.Match(line);
                    if (match.Success)
                    {
                        string relativePath = Path.GetRelativePath(srcDir, file);
                        violations.Add($"{relativePath}:{i + 1}: Found RightToLeft FlowDirection '{match.Value}' in line: {line.Trim()}");
                    }
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} Right-to-Left FlowDirection violations (Strict English LTR required):\n" +
            string.Join("\n", violations));
    }
}
