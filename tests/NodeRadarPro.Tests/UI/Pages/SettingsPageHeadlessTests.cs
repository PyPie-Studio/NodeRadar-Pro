using LiteDB;
using NodeRadarPro.Data;

namespace NodeRadarPro.Tests.UI.Pages;

[Collection("AvaloniaUIStaticState")]
public class SettingsPageHeadlessTests : IDisposable
{
    private readonly MemoryStream _ms;
    private readonly LiteDatabase _liteDb;
    private readonly LocalDatabase _db;

    public SettingsPageHeadlessTests()
    {
        AvaloniaTestApp.EnsureInitialized();
        _ms = new MemoryStream();
        _liteDb = new LiteDatabase(_ms);
        _db = new LocalDatabase(_liteDb);
    }

    public void Dispose()
    {
        _db.Dispose();
        _liteDb.Dispose();
        _ms.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void SettingsPage_InitialLoad_LoadsDatabaseSettings()
    {
        var custom = new AppSettings
        {
            SweepFrequencySeconds = 45,
            ResponseTimeoutMs = 2500,
            LatencyThresholdMs = 250,
            PacketLossThresholdPct = 5.0,
            EnableSynScan = true,
            EnableEmailAlerts = true,
            SmtpHost = "mail.internal.corp",
            SmtpPort = 465,
            LogRetentionDays = 14,
            ResolvedAlertRetentionDays = 7
        };
        _db.SaveSettings(custom);

        var page = new SettingsPage(_db);

        Assert.Equal(45, page.SweepFrequencySlider.Value);
        Assert.Equal("45s", page.SweepFrequencyValue.Text);
        Assert.Equal(2500, page.ResponseTimeoutSlider.Value);
        Assert.Equal("2500ms", page.ResponseTimeoutValue.Text);
        Assert.True(page.SynScanToggle.IsChecked);
        Assert.True(page.EmailToggle.IsChecked);
        Assert.True(page.SmtpSettingsPanel.IsVisible);
        Assert.Equal("mail.internal.corp", page.SmtpHostInput.Text);
        Assert.Equal("465", page.SmtpPortInput.Text);
    }

    [Fact]
    public void SettingsPage_SliderChanges_UpdatesFormattedLabels()
    {
        var page = new SettingsPage(_db);

        page.SweepFrequencySlider.Value = 60;
        Assert.Equal("60s", page.SweepFrequencyValue.Text);

        page.ResponseTimeoutSlider.Value = 3000;
        Assert.Equal("3000ms", page.ResponseTimeoutValue.Text);

        page.AutoBackupIntervalSlider.Value = 48;
        Assert.Equal("48h", page.AutoBackupIntervalValue.Text);

        page.LogRetentionSlider.Value = 60;
        Assert.Equal("60d", page.LogRetentionValue.Text);

        page.AlertRetentionSlider.Value = 45;
        Assert.Equal("45d", page.AlertRetentionValue.Text);
    }

    [Fact]
    public void SettingsPage_EmailToggle_ControlsSmtpPanelVisibility()
    {
        var page = new SettingsPage(_db);

        page.EmailToggle.IsChecked = false;
        Assert.False(page.SmtpSettingsPanel.IsVisible);

        page.EmailToggle.IsChecked = true;
        Assert.True(page.SmtpSettingsPanel.IsVisible);

        page.EmailToggle.IsChecked = false;
        Assert.False(page.SmtpSettingsPanel.IsVisible);
    }

    [Fact]
    public void SettingsPage_SaveConfiguration_PersistsChangesToDatabase()
    {
        var page = new SettingsPage(_db);

        page.SweepFrequencySlider.Value = 15;
        page.ResponseTimeoutSlider.Value = 800;
        page.SynScanToggle.IsChecked = true;
        page.DnsResolveToggle.IsChecked = false;
        page.SmtpHostInput.Text = "smtp.custom.org";
        page.SmtpPortInput.Text = "2525";

        page.TriggerSave();

        var saved = _db.LoadSettings();
        Assert.Equal(15, saved.SweepFrequencySeconds);
        Assert.Equal(800, saved.ResponseTimeoutMs);
        Assert.True(saved.EnableSynScan);
        Assert.False(saved.EnableDnsResolve);
        Assert.Equal("smtp.custom.org", saved.SmtpHost);
        Assert.Equal(2525, saved.SmtpPort);
    }

    [Fact]
    public void SettingsPage_ResetDefaultsLatch_RequiresDoubleConfirmation()
    {
        _db.SaveSettings(new AppSettings { SweepFrequencySeconds = 120 });
        var page = new SettingsPage(_db);
        Assert.Equal(120, page.SweepFrequencySlider.Value);

        // First click arms reset button
        page.TriggerReset();
        Assert.Contains("Confirm", page.ResetButton.Content?.ToString() ?? "");

        // Second click confirms and resets
        page.TriggerReset();
        var reloaded = _db.LoadSettings();
        Assert.Equal(30, reloaded.SweepFrequencySeconds);
        Assert.Equal(30, page.SweepFrequencySlider.Value);
    }

    [Fact]
    public void SettingsPage_PruneLatch_ArmsAndConfirms()
    {
        var page = new SettingsPage(_db);

        // First click arms prune button
        page.TriggerPrune();
        Assert.NotNull(page.PruneButton.Content);

        // Second click executes prune
        page.TriggerPrune();
        Assert.False(page.PruneButton.IsEnabled);
        Assert.Equal("Pruning expired database records...", page.MaintenanceStatus.Text);
    }
}
