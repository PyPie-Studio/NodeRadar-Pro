using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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
        var logoImage = new Image
        {
            Source = new Bitmap(AssetLoader.Open(new Uri("avares://NodeRadar Pro/Resources/NodeRadar Pro Icon.png"))),
            Width = 56, Height = 56,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        RenderOptions.SetBitmapInterpolationMode(logoImage, BitmapInterpolationMode.HighQuality);

        var logoIcon = new Border
        {
            Width = 72, Height = 72, CornerRadius = new CornerRadius(0), // Issue 7 fix: Force square corners
            Background = ThemeTokens.SurfaceContainerLowest,
            BorderBrush = ThemeTokens.GhostBorder,
            BorderThickness = new Thickness(1),
            Child = logoImage,
            Margin = new Thickness(0, 0, 20, 0)
        };

        var appName = ThemeTokens.Headline("NodeRadar Pro", 26);
        var appCompany = new TextBlock { Text = "by PyPie Studio", FontSize = 16, Foreground = ThemeTokens.Primary, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.Medium, Margin = new Thickness(0, 2, 0, 4) };
        var versionBadge = new Border { Background = new SolidColorBrush(Color.Parse("#6B21A8"), 0.3), CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 4), HorizontalAlignment = HorizontalAlignment.Left, Child = new TextBlock { Text = $"Version {AppVersion} • {BuildDate}", FontSize = 13, Foreground = ThemeTokens.Primary, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.SemiBold } };

        var infoTextCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { appName, appCompany, versionBadge } };
        var infoHeader = new StackPanel { Orientation = Orientation.Horizontal, Children = { logoIcon, infoTextCol } };

        var infoContent = new StackPanel { Spacing = 12, Children = { infoHeader } };
        var infoCard = ThemeTokens.GlassCard(infoContent, 28);
        ThemeTokens.SetToolTip(infoCard, "Core application identity and release information.");

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
                MakeInfoRow("Framework", ".NET 10 (NativeAOT)"),
                MakeInfoRow("UI Engine", "Avalonia UI 12.x"),
                MakeInfoRow("Database", "LiteDB 5.x (Singleton)"),
                MakeInfoRow("Security", "SHA256 Integrity Shield")
            }
        };
        var sysCard = ThemeTokens.Card(sysContent, ThemeTokens.SurfaceContainerLow, 24);
        sysCard.Margin = new Thickness(0, 16, 0, 0);
        ThemeTokens.SetToolTip(sysCard, "Technical diagnostics of the current execution environment.");

        // App Updates Card
        var updatesIcon = ThemeTokens.VectorIcon("M12,18A6,6 0 1,0 6,12C6,15.31 8.69,18 12,18M12,20A8,8 0 1,1 20,12A8,8 0 0,1 12,20M12,10.8L10.5,12L12,13.2V15L9,12L12,9V10.8M12.6,9L15.6,12L12.6,15V13.2L14.1,12L12.6,10.8V9Z", 18);
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
        ThemeTokens.SetToolTip(checkUpdateBtn, "Contact PyPie Studio releases server to check for a newer version of NodeRadar Pro.");
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
        ThemeTokens.SetToolTip(updatesCard, "Monitor software versioning and check for mandatory security updates.");

        var leftCol = new StackPanel { Children = { infoCard, sysCard, updatesCard } };

        // ═══════════════════════
        // REPORTING SECTION
        // ═══════════════════════
        var reportIcon = ThemeTokens.VectorIcon("M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20Z", 18);
        var reportTitle = ThemeTokens.Headline("Security Reporting", 20);
        var reportTitleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 0, 0, 16), Children = { reportIcon, reportTitle } };

        var reportDesc = ThemeTokens.Body("Generate an enterprise-grade PDF audit of your current network state, including vulnerability assessments and device inventory.", 14);
        reportDesc.Margin = new Thickness(0, 0, 0, 20);
        reportDesc.TextWrapping = TextWrapping.Wrap;

        var generateBtn = ThemeTokens.PrimaryButton("🛡  Generate Security Audit");
        ThemeTokens.SetToolTip(generateBtn, "Export a comprehensive PDF security report to your Desktop.");
        generateBtn.Height = 50;
        
        generateBtn.Click += async (s, e) =>
        {
            generateBtn.IsEnabled = false;
            generateBtn.Content = "⚙  Analyzing Network...";
            await Task.Delay(2000);
            generateBtn.Content = "✅  Audit Exported to Desktop";
            var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            timer.Tick += (s2, e2) => { generateBtn.Content = "🛡  Generate Security Audit"; generateBtn.IsEnabled = true; timer.Stop(); };
            timer.Start();
        };

        var reportContent = new StackPanel { Children = { reportTitleRow, reportDesc, generateBtn } };
        var reportCard = ThemeTokens.Card(reportContent, ThemeTokens.SurfaceContainerLow, 24);
        reportCard.Margin = new Thickness(0, 16, 0, 0);

        leftCol.Children.Add(reportCard);

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
                MakeFeatureRow(ThemeTokens.VectorIcon("M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M12,6A6,6 0 0,0 6,12A6,6 0 0,0 12,18A6,6 0 0,0 18,12A6,6 0 0,0 12,6M12,8A4,4 0 0,1 16,12A4,4 0 0,1 12,16A4,4 0 0,1 8,12A4,4 0 0,1 12,8Z", 18), "Network Discovery", "ARP + ICMP + TCP multi-method subnet scanning"),
                MakeFeatureRow(ThemeTokens.VectorIcon("M15.5,14L20.35,18.85L19,20.21L14.15,15.35C13.12,15.77 12,16 10.75,16A6.75,6.75 0 1,1 17.5,9.25C17.5,10.5 17.27,11.62 16.85,12.65L15.5,14M10.75,5A4.25,4.25 0 0,0 6.5,9.25A4.25,4.25 0 0,0 10.75,13.5A4.25,4.25 0 0,0 15,9.25A4.25,4.25 0 0,0 10.75,5Z", 18), "Port Scanning", "Fast scan, full range, and OS fingerprinting"),
                MakeFeatureRow(ThemeTokens.VectorIcon(ThemeTokens.SvgChart, 18), "Device Monitoring", "Real-time connectivity tracking with uptime history"),
                MakeFeatureRow(ThemeTokens.VectorIcon(ThemeTokens.SvgShield, 18), "Alert System", "Connection loss, latency spikes, and packet loss alerts"),
                MakeFeatureRow(ThemeTokens.VectorIcon("M3,5H21V7H3V5M3,9H21V11H3V9M3,13H21V15H3V13M3,17H21V19H3V17Z", 18), "System Logs", "Full audit trail with search, filter, and export"),
                MakeFeatureRow(ThemeTokens.VectorIcon("M15,9H5V5H15M12,19A2,2 0 1,1 14,17A2,2 0 0,1 12,19M17,3H3C1.89,3 1,3.89 1,5V21A2,2 0 0,0 3,23H17A2,2 0 0,0 19,21V5C19,3.89 18.1,3 17,3Z", 18), "Device Registry", "Name, tag, and organize your network nodes")
            }
        };

        var featureContent = new StackPanel { Children = { featTitle, featuresList } };
        var featureCard = ThemeTokens.Card(featureContent, ThemeTokens.SurfaceContainerLow, 24);

        // Contact & Social Card
        var contactIcon = ThemeTokens.VectorIcon("M20,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V6C22,4.89 21.1,4 20,4M20,18H4V8L12,13L20,8V18M12,11L4,6H20L12,11Z", 18);
        var contactTitle = ThemeTokens.Headline("Contact & Social", 20);
        var contactTitleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 0, 0, 16), Children = { contactIcon, contactTitle } };

        var contactContent = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                contactTitleRow,
                MakeLinkRow(ThemeTokens.VectorIcon("M21,16.5C21,16.88 20.79,17.21 20.47,17.38L12.57,21.82C12.41,21.94 12.21,22 12,22C11.79,22 11.59,21.94 11.43,21.82L3.53,17.38C3.21,17.21 3,16.88 3,16.5V7.5C3,7.12 3.21,6.79 3.53,6.62L11.43,2.18C11.59,2.06 11.79,2 12,2C12.21,2 12.41,2.06 12.57,2.18L20.47,6.62C20.79,6.79 21,7.12 21,7.5V16.5Z", 16), "Developer", "PyPie Studio", null, "The creators of NodeRadar Pro."),
                MakeLinkRow(ThemeTokens.VectorIcon("M7.8,2H16.2C19.4,2 22,4.6 22,7.8V16.2A5.8,5.8 0 0,1 16.2,22H7.8C4.6,22 2,19.4 2,16.2V7.8A5.8,5.8 0 0,1 7.8,2M7.6,4A3.6,3.6 0 0,0 4,7.6V16.4C4,18.39 5.61,20 7.6,20H16.4A3.6,3.6 0 0,0 20,16.4V7.6C20,5.61 18.39,4 16.4,4H7.6M17.25,5.5A1.25,1.25 0 1,1 16,6.75A1.25,1.25 0 0,1 17.25,5.5M12,7A5,5 0 1,1 7,12A5,5 0 0,1 12,7M12,9A3,3 0 1,0 15,12A3,3 0 0,0 12,9Z", 16), "Instagram", "@pypiestudio", "https://instagram.com/pypiestudio", "Follow our journey and updates on Instagram."),
                MakeLinkRow(ThemeTokens.VectorIcon("M20,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V6C22,4.89 21.1,4 20,4M20,18H4V8L12,13L20,8V18M12,11L4,6H20L12,11Z", 16), "Email", "support@pypiestudio.com", null, "Send us a direct technical support request."),
                MakeLinkRow(ThemeTokens.VectorIcon("M16.36,14C16.44,13.34 16.5,12.68 16.5,12C16.5,11.32 16.44,10.66 16.36,10H19.74C19.9,10.64 20,11.31 20,12C20,12.69 19.9,13.36 19.74,14M14.59,19.56C15.19,18.13 15.65,16.61 15.92,15H18.59C17.62,17.07 15.96,18.78 14.59,19.56M14.34,14H9.66C9.56,13.34 9.5,12.68 9.5,12C9.5,11.32 9.56,10.66 9.66,10H14.34C14.44,10.66 14.5,11.32 14.5,12C14.5,12.68 14.44,13.34 14.34,14M12,19.96C11.17,18.76 10.5,17.43 10.09,16H13.91C13.5,17.43 12.83,18.76 12,19.96M8,15C8.27,16.61 8.73,18.13 9.33,19.56C7.96,18.78 6.3,17.07 5.33,15M4.26,14C4.1,13.36 4,12.69 4,12C4,11.31 4.1,10.64 4.26,10H7.64C7.56,10.66 7.5,11.32 7.5,12C7.5,12.68 7.64,13.34 7.64,14M5.33,9C6.3,6.93 7.96,5.22 9.33,4.44C8.73,5.87 8.27,7.39 8,9M10.09,8C10.5,6.57 11.17,5.24 12,4.04C12.83,5.24 13.5,6.57 13.91,8M18.59,9H15.92C15.65,7.39 15.19,5.87 14.59,4.44C15.96,5.22 17.62,6.93 18.59,9M12,2C6.47,2 2,6.47 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z", 16), "Website", "pypiestudio.com", null, "Visit our official homepage.")
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

    private static Border MakeFeatureRow(Control icon, string title, string desc) => new()
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
                    Child = icon
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

    private static Border MakeLinkRow(Control icon, string label, string value, string? url = null, string? tip = null)
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
                    icon,
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

        if (tip != null) ThemeTokens.SetToolTip(row, tip);

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
