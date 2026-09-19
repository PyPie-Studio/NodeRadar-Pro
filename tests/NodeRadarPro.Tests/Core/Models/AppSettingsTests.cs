using System.Text.Json;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

public class AppSettingsTests
{
    [Fact]
    public void SerializeAndDeserialize_DefaultValues_PreservesData()
    {
        // Arrange
        var originalSettings = new AppSettings();

        // Act
        string json = JsonSerializer.Serialize(originalSettings);
        var deserializedSettings = JsonSerializer.Deserialize<AppSettings>(json);

        // Assert
        Assert.NotNull(deserializedSettings);
        Assert.Equal(originalSettings.Id, deserializedSettings.Id);
        Assert.Equal(originalSettings.MonitorIntervalSeconds, deserializedSettings.MonitorIntervalSeconds);
        Assert.Equal(originalSettings.EnableOfflineFadeOut, deserializedSettings.EnableOfflineFadeOut);
        Assert.Equal(originalSettings.FadeOutSeconds, deserializedSettings.FadeOutSeconds);
        Assert.Equal(originalSettings.SweepFrequencySeconds, deserializedSettings.SweepFrequencySeconds);
        Assert.Equal(originalSettings.ResponseTimeoutMs, deserializedSettings.ResponseTimeoutMs);
        Assert.Equal(originalSettings.EnableSynScan, deserializedSettings.EnableSynScan);
        Assert.Equal(originalSettings.EnableDnsResolve, deserializedSettings.EnableDnsResolve);
        Assert.Equal(originalSettings.EnableFastScan, deserializedSettings.EnableFastScan);
        Assert.Equal(originalSettings.EnableOsDetection, deserializedSettings.EnableOsDetection);
        Assert.Equal(originalSettings.EnableInlinePortScan, deserializedSettings.EnableInlinePortScan);
        Assert.Equal(originalSettings.SelectedInterfaceName, deserializedSettings.SelectedInterfaceName);
        Assert.Equal(originalSettings.EnablePromiscuous, deserializedSettings.EnablePromiscuous);
        Assert.Equal(originalSettings.LatencyThresholdMs, deserializedSettings.LatencyThresholdMs);
        Assert.Equal(originalSettings.PacketLossThresholdPct, deserializedSettings.PacketLossThresholdPct);
        Assert.Equal(originalSettings.EnableToastAlerts, deserializedSettings.EnableToastAlerts);
        Assert.Equal(originalSettings.EnableSoundAlerts, deserializedSettings.EnableSoundAlerts);
        Assert.Equal(originalSettings.EnableEmailAlerts, deserializedSettings.EnableEmailAlerts);
        Assert.Equal(originalSettings.SmtpHost, deserializedSettings.SmtpHost);
        Assert.Equal(originalSettings.SmtpPort, deserializedSettings.SmtpPort);
        Assert.Equal(originalSettings.SmtpEmail, deserializedSettings.SmtpEmail);
        Assert.Equal(originalSettings.SmtpUser, deserializedSettings.SmtpUser);
        Assert.Equal(originalSettings.SmtpPassword, deserializedSettings.SmtpPassword);
        Assert.Equal(originalSettings.EnableAutoBackup, deserializedSettings.EnableAutoBackup);
        Assert.Equal(originalSettings.AutoBackupIntervalHours, deserializedSettings.AutoBackupIntervalHours);
        Assert.Equal(originalSettings.LastKnownGoodHash, deserializedSettings.LastKnownGoodHash);
    }

    [Fact]
    public void SerializeAndDeserialize_CustomValues_PreservesData()
    {
        // Arrange
        var originalSettings = new AppSettings
        {
            Id = 42,
            MonitorIntervalSeconds = 120,
            EnableOfflineFadeOut = true,
            FadeOutSeconds = 600,
            SweepFrequencySeconds = 15,
            ResponseTimeoutMs = 3000,
            EnableSynScan = true,
            EnableDnsResolve = false,
            EnableFastScan = true,
            EnableOsDetection = false,
            EnableInlinePortScan = true,
            SelectedInterfaceName = "eth1",
            EnablePromiscuous = true,
            LatencyThresholdMs = 500,
            PacketLossThresholdPct = 10.5,
            EnableToastAlerts = false,
            EnableSoundAlerts = false,
            EnableEmailAlerts = true,
            SmtpHost = "smtp.example.com",
            SmtpPort = 465,
            SmtpEmail = "alert@example.com",
            SmtpUser = "user1",
            SmtpPassword = "password123",
            EnableAutoBackup = false,
            AutoBackupIntervalHours = 12,
            LastKnownGoodHash = "abcdef"
        };

        // Act
        string json = JsonSerializer.Serialize(originalSettings);
        var deserializedSettings = JsonSerializer.Deserialize<AppSettings>(json);

        // Assert
        Assert.NotNull(deserializedSettings);
        Assert.Equal(originalSettings.Id, deserializedSettings.Id);
        Assert.Equal(originalSettings.MonitorIntervalSeconds, deserializedSettings.MonitorIntervalSeconds);
        Assert.Equal(originalSettings.EnableOfflineFadeOut, deserializedSettings.EnableOfflineFadeOut);
        Assert.Equal(originalSettings.FadeOutSeconds, deserializedSettings.FadeOutSeconds);
        Assert.Equal(originalSettings.SweepFrequencySeconds, deserializedSettings.SweepFrequencySeconds);
        Assert.Equal(originalSettings.ResponseTimeoutMs, deserializedSettings.ResponseTimeoutMs);
        Assert.Equal(originalSettings.EnableSynScan, deserializedSettings.EnableSynScan);
        Assert.Equal(originalSettings.EnableDnsResolve, deserializedSettings.EnableDnsResolve);
        Assert.Equal(originalSettings.EnableFastScan, deserializedSettings.EnableFastScan);
        Assert.Equal(originalSettings.EnableOsDetection, deserializedSettings.EnableOsDetection);
        Assert.Equal(originalSettings.EnableInlinePortScan, deserializedSettings.EnableInlinePortScan);
        Assert.Equal(originalSettings.SelectedInterfaceName, deserializedSettings.SelectedInterfaceName);
        Assert.Equal(originalSettings.EnablePromiscuous, deserializedSettings.EnablePromiscuous);
        Assert.Equal(originalSettings.LatencyThresholdMs, deserializedSettings.LatencyThresholdMs);
        Assert.Equal(originalSettings.PacketLossThresholdPct, deserializedSettings.PacketLossThresholdPct);
        Assert.Equal(originalSettings.EnableToastAlerts, deserializedSettings.EnableToastAlerts);
        Assert.Equal(originalSettings.EnableSoundAlerts, deserializedSettings.EnableSoundAlerts);
        Assert.Equal(originalSettings.EnableEmailAlerts, deserializedSettings.EnableEmailAlerts);
        Assert.Equal(originalSettings.SmtpHost, deserializedSettings.SmtpHost);
        Assert.Equal(originalSettings.SmtpPort, deserializedSettings.SmtpPort);
        Assert.Equal(originalSettings.SmtpEmail, deserializedSettings.SmtpEmail);
        Assert.Equal(originalSettings.SmtpUser, deserializedSettings.SmtpUser);
        Assert.Equal(originalSettings.SmtpPassword, deserializedSettings.SmtpPassword);
        Assert.Equal(originalSettings.EnableAutoBackup, deserializedSettings.EnableAutoBackup);
        Assert.Equal(originalSettings.AutoBackupIntervalHours, deserializedSettings.AutoBackupIntervalHours);
        Assert.Equal(originalSettings.LastKnownGoodHash, deserializedSettings.LastKnownGoodHash);
    }
}
