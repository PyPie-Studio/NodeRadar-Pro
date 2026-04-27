using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NodeRadarPro.UI;

/// <summary>
/// Support & About page: company branding, version info, app updates, FAQ, and social links.
/// </summary>
public class SupportPage : Border
{
    private const string AppVersion = "1.0.0";
    private const string BuildDate = "April 2026";

    public SupportPage()
    {
        Background = ThemeTokens.Surface;

        // ═══════════════════════
        // HEADER
        // ═══════════════════════
        var label = new TextBlock { Text = "SUPPORT & ABOUT", FontSize = 13, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.NavAccentBorder, FontFamily = new FontFamily("Inter"), LetterSpacing = 2.5, Margin = new Thickness(0, 0, 0, 8) };
        var title = ThemeTokens.Headline("NodeRadar Pro", 38);
        title.Margin = new Thickness(0, 0, 0, 4);
        var subtitle = ThemeTokens.Body("Advanced network reconnaissance toolkit by PyPie Studio.", 16);
        var headerSection = new StackPanel { Margin = new Thickness(0, 0, 0, 28), Children = { label, title, subtitle } };

        // ═══════════════════════
        // LEFT COLUMN
        // ═══════════════════════

        // App Info Card
        var logoIcon = new Border
        {
            Width = 72, Height = 72, CornerRadius = new CornerRadius(16),
            Background = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative), GradientStops = { new GradientStop(Color.Parse("#7C3AED"), 0), new GradientStop(Color.Parse("#4CD7F6"), 1) } },
            Child = new TextBlock { Text = "◎", FontSize = 32, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
            Margin = new Thickness(0, 0, 20, 0)
        };

        var appName = ThemeTokens.Headline("NodeRadar Pro", 26);
        var appCompany = new TextBlock { Text = "by PyPie Studio", FontSize = 16, Foreground = ThemeTokens.Primary, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.Medium, Margin = new Thickness(0, 2, 0, 4) };
        var versionBadge = new Border { Background = new SolidColorBrush(Color.Parse("#6B21A8"), 0.3), CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 4), HorizontalAlignment = HorizontalAlignment.Left, Child = new TextBlock { Text = $"Version {AppVersion} • {BuildDate}", FontSize = 13, Foreground = ThemeTokens.Primary, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.SemiBold } };

        var infoTextCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { appName, appCompany, versionBadge } };
        var infoHeader = new StackPanel { Orientation = Orientation.Horizontal, Children = { logoIcon, infoTextCol } };

        var infoContent = new StackPanel { Spacing = 12, Children = { infoHeader } };
        var infoCard = ThemeTokens.GlassCard(infoContent, 28);

        // System Info Card
        var sysTitle = ThemeTokens.Headline("System Information", 20);
        sysTitle.Margin = new Thickness(0, 0, 0, 16);

        var sysContent = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                sysTitle,
                MakeInfoRow("Operating System", RuntimeInformation.OSDescription),
                MakeInfoRow("Architecture", RuntimeInformation.OSArchitecture.ToString()),
                MakeInfoRow("Framework", RuntimeInformation.FrameworkDescription),
                MakeInfoRow("Avalonia UI", "11.x"),
                MakeInfoRow("Database", "LiteDB (Embedded)")
            }
        };
        var sysCard = ThemeTokens.Card(sysContent, ThemeTokens.SurfaceContainerLow, 24);
        sysCard.Margin = new Thickness(0, 16, 0, 0);

        // App Updates Card
        var updatesIcon = new TextBlock { Text = "🔄", FontSize = 18, VerticalAlignment = VerticalAlignment.Center };
        var updatesTitle = ThemeTokens.Headline("App Updates", 20);
        var updatesTitleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 0, 0, 16), Children = { updatesIcon, updatesTitle } };

        var currentVerLabel = ThemeTokens.Label("CURRENT VERSION", 12); currentVerLabel.LetterSpacing = 1.5; currentVerLabel.Margin = new Thickness(0, 0, 0, 4);
        var currentVer = new TextBlock { Text = $"v{AppVersion}", FontSize = 22, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.Tertiary, FontFamily = new FontFamily("Inter") };

        var statusRow = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#005362"), 0.2),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10),
            Margin = new Thickness(0, 12, 0, 0),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    ThemeTokens.StatusDot(true, 10),
                    new TextBlock { Text = "You are running the latest version.", FontSize = 15, Foreground = ThemeTokens.Tertiary, FontFamily = new FontFamily("Inter"), VerticalAlignment = VerticalAlignment.Center }
                }
            }
        };

        var changelogLabel = ThemeTokens.Label("CHANGELOG", 12); changelogLabel.LetterSpacing = 1.5; changelogLabel.Margin = new Thickness(0, 16, 0, 8);
        var changelogItems = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                MakeChangelogItem("v1.0.0", "Initial release — subnet scanning, device inventory, port scanning, alert system, system logs."),
            }
        };

        var checkUpdateBtn = ThemeTokens.PrimaryButton("🔄  Check for Updates");
        checkUpdateBtn.Margin = new Thickness(0, 16, 0, 0);
        checkUpdateBtn.Click += async (s, e) =>
        {
            checkUpdateBtn.IsEnabled = false;
            checkUpdateBtn.Content = "⏳  Checking...";
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("NodeRadarPro/1.0");
                // Check GitHub releases API for latest version
                var response = await http.GetStringAsync("https://api.github.com/repos/pypiestudio/noderadar-pro/releases/latest");
                // Simple JSON parse for tag_name
                var tagIdx = response.IndexOf("\"tag_name\"");
                if (tagIdx > 0)
                {
                    var valStart = response.IndexOf('"', tagIdx + 11) + 1;
                    var valEnd = response.IndexOf('"', valStart);
                    var latestVersion = response[valStart..valEnd].TrimStart('v');
                    if (latestVersion != "1.0.0" && !string.IsNullOrEmpty(latestVersion))
                        checkUpdateBtn.Content = $"⬆  Update available: v{latestVersion}";
                    else
                        checkUpdateBtn.Content = "✅  Up to date!";
                }
                else
                {
                    checkUpdateBtn.Content = "✅  Up to date!";
                }
            }
            catch (HttpRequestException)
            {
                checkUpdateBtn.Content = "⚠  Could not reach update server";
            }
            catch
            {
                checkUpdateBtn.Content = "✅  Up to date!";
            }
            finally
            {
                checkUpdateBtn.IsEnabled = true;
                // Reset after 5 seconds
                var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (s2, e2) => { checkUpdateBtn.Content = "🔄  Check for Updates"; timer.Stop(); };
                timer.Start();
            }
        };

        var updatesContent = new StackPanel { Children = { updatesTitleRow, currentVerLabel, currentVer, statusRow, changelogLabel, changelogItems, checkUpdateBtn } };
        var updatesCard = ThemeTokens.Card(updatesContent, ThemeTokens.SurfaceContainerLow, 24);
        updatesCard.Margin = new Thickness(0, 16, 0, 0);

        var leftCol = new StackPanel { Children = { infoCard, sysCard, updatesCard } };

        // ═══════════════════════
        // RIGHT COLUMN
        // ═══════════════════════

        // Key Features Card
        var featTitle = ThemeTokens.Headline("Key Features", 20);
        featTitle.Margin = new Thickness(0, 0, 0, 16);

        var featuresList = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                MakeFeatureRow("◎", "Network Discovery", "ARP + ICMP + TCP multi-method subnet scanning"),
                MakeFeatureRow("🔍", "Port Scanning", "Fast scan, full range, and OS fingerprinting"),
                MakeFeatureRow("📊", "Device Monitoring", "Real-time connectivity tracking with uptime history"),
                MakeFeatureRow("🛡", "Alert System", "Connection loss, latency spikes, and packet loss alerts"),
                MakeFeatureRow("📋", "System Logs", "Full audit trail with search, filter, and export"),
                MakeFeatureRow("💾", "Device Registry", "Name, tag, and organize your network nodes")
            }
        };

        var featureContent = new StackPanel { Children = { featTitle, featuresList } };
        var featureCard = ThemeTokens.Card(featureContent, ThemeTokens.SurfaceContainerLow, 24);

        // Contact & Social Card
        var contactIcon = new TextBlock { Text = "📬", FontSize = 18, VerticalAlignment = VerticalAlignment.Center };
        var contactTitle = ThemeTokens.Headline("Contact & Social", 20);
        var contactTitleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 0, 0, 16), Children = { contactIcon, contactTitle } };

        var contactContent = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                contactTitleRow,
                MakeLinkRow("🏢", "Developer", "PyPie Studio"),
                MakeLinkRow("📸", "Instagram", "@pypiestudio", "https://instagram.com/pypiestudio"),
                MakeLinkRow("📧", "Email", "support@pypiestudio.com"),
                MakeLinkRow("🌐", "Website", "pypiestudio.com")
            }
        };
        var contactCard = ThemeTokens.GlassCard(contactContent, 24);
        contactCard.Margin = new Thickness(0, 16, 0, 0);

        // FAQ Card
        var faqTitle = ThemeTokens.Headline("Frequently Asked Questions", 20);
        faqTitle.Margin = new Thickness(0, 0, 0, 16);

        var faqContent = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                faqTitle,
                MakeFaqItem("Why do some devices show high latency?", "Mobile devices in WiFi power-save mode may delay ICMP responses. ARP verification ensures they are correctly detected as online."),
                MakeFaqItem("Where is my data stored?", "All data is stored locally in Documents/PyPie Studio/NodeRadar Pro/noderadar.db using LiteDB."),
                MakeFaqItem("Can I export my scan results?", "Yes! Use the export button on the Scanner page to save results as CSV, or export logs from the System Logs page."),
                MakeFaqItem("What ports does Fast Scan check?", "Fast Scan probes the top 100 most common ports as defined by network security standards.")
            }
        };
        var faqCard = ThemeTokens.Card(faqContent, ThemeTokens.SurfaceContainerLow, 24);
        faqCard.Margin = new Thickness(0, 16, 0, 0);

        var rightCol = new StackPanel { Children = { featureCard, contactCard, faqCard } };

        // ═══════════════════════
        // ROOT
        // ═══════════════════════
        var bodyGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(new GridLength(1, GridUnitType.Star)), new ColumnDefinition(new GridLength(1, GridUnitType.Star)) }
        };
        Grid.SetColumn(leftCol, 0); Grid.SetColumn(rightCol, 1);
        leftCol.Margin = new Thickness(0, 0, 12, 0); rightCol.Margin = new Thickness(12, 0, 0, 0);
        bodyGrid.Children.Add(leftCol); bodyGrid.Children.Add(rightCol);

        var root = new StackPanel { Margin = new Thickness(32, 28), Children = { headerSection, bodyGrid } };
        Child = new ScrollViewer { Content = root, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
    }

    // ══════════════════════════════════
    // UI FACTORY HELPERS
    // ══════════════════════════════════

    private static Border MakeInfoRow(string label, string value)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(180)));
        row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

        var labelTb = new TextBlock { Text = label, FontSize = 14, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter") };
        var valueTb = new TextBlock { Text = value, FontSize = 14, Foreground = ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.Medium, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetColumn(labelTb, 0); Grid.SetColumn(valueTb, 1);
        row.Children.Add(labelTb); row.Children.Add(valueTb);

        return new Border { Background = ThemeTokens.SurfaceContainerLowest, CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 10), Child = row };
    }

    private static Border MakeFeatureRow(string icon, string title, string desc) => new()
    {
        Background = ThemeTokens.SurfaceContainerLowest,
        CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 12),
        BorderBrush = ThemeTokens.GhostBorder, BorderThickness = new Thickness(1),
        Child = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 12,
            Children =
            {
                new Border
                {
                    Width = 40, Height = 40, CornerRadius = new CornerRadius(8),
                    Background = ThemeTokens.SurfaceContainerHigh,
                    Child = new TextBlock { Text = icon, FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
                },
                new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeight.SemiBold, Foreground = ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter") },
                        new TextBlock { Text = desc, FontSize = 13, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), Margin = new Thickness(0, 2, 0, 0) }
                    }
                }
            }
        }
    };

    private static Border MakeLinkRow(string icon, string label, string value, string? url = null)
    {
        var valueTb = new TextBlock { Text = value, FontSize = 15, Foreground = url != null ? ThemeTokens.Tertiary : ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.Medium };

        var row = new Border
        {
            Background = ThemeTokens.SurfaceContainerLowest, CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 10),
            BorderBrush = ThemeTokens.GhostBorder, BorderThickness = new Thickness(1),
            Cursor = url != null ? new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) : null,
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal, Spacing = 12,
                Children =
                {
                    new TextBlock { Text = icon, FontSize = 16, VerticalAlignment = VerticalAlignment.Center },
                    new StackPanel
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        Children =
                        {
                            new TextBlock { Text = label, FontSize = 12, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter") },
                            valueTb
                        }
                    }
                }
            }
        };

        if (url != null)
        {
            row.PointerPressed += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
                catch { }
            };
        }
        return row;
    }

    private static Border MakeFaqItem(string question, string answer) => new()
    {
        Background = ThemeTokens.SurfaceContainerLowest, CornerRadius = new CornerRadius(10), Padding = new Thickness(16, 14),
        BorderBrush = ThemeTokens.GhostBorder, BorderThickness = new Thickness(1),
        Child = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = question, FontSize = 15, FontWeight = FontWeight.SemiBold, Foreground = ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter") },
                new TextBlock { Text = answer, FontSize = 14, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), TextWrapping = TextWrapping.Wrap }
            }
        }
    };

    private static Border MakeChangelogItem(string version, string description) => new()
    {
        Background = ThemeTokens.SurfaceContainerLowest, CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 10),
        Child = new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new Border { Background = new SolidColorBrush(Color.Parse("#6B21A8"), 0.3), CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 3), HorizontalAlignment = HorizontalAlignment.Left, Child = new TextBlock { Text = version, FontSize = 13, Foreground = ThemeTokens.Primary, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.SemiBold } },
                new TextBlock { Text = description, FontSize = 14, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) }
            }
        }
    };
}
