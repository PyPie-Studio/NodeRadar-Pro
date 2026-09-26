using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

public class AudioServiceTests : IDisposable
{
    private readonly string _testLogDir;

    public AudioServiceTests()
    {
        AudioService.Enabled = true;
        AudioService.SetSoundPlayerForTesting(null);
        _testLogDir = Path.Combine(Path.GetTempPath(), "NodeRadarAudioTest_" + Guid.NewGuid());
        Directory.CreateDirectory(_testLogDir);
        Logger.SetCustomLogDirectoryForTesting(_testLogDir);
    }

    public void Dispose()
    {
        AudioService.Enabled = true;
        AudioService.SetSoundPlayerForTesting(null);
        Logger.SetCustomLogDirectoryForTesting(null);
        try
        {
            if (Directory.Exists(_testLogDir))
                Directory.Delete(_testLogDir, true);
        }
        catch { }
    }

    [Fact]
    public void Enabled_CanBeSetAndRead()
    {
        AudioService.Enabled = false;
        Assert.False(AudioService.Enabled);

        AudioService.Enabled = true;
        Assert.True(AudioService.Enabled);
    }

    [Fact]
    public void PlayAlert_WhenDisabled_DoesNotPlaySound()
    {
        bool played = false;
        AudioService.SetSoundPlayerForTesting(_ => played = true);
        AudioService.Enabled = false;

        AudioService.PlayAlert(true);
        AudioService.PlayAlert(false);

        Assert.False(played);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PlayAlert_WhenEnabled_CallsSoundPlayerWithCorrectCriticalFlag(bool critical)
    {
        bool? playedCritical = null;
        AudioService.SetSoundPlayerForTesting(c => playedCritical = c);
        AudioService.Enabled = true;

        AudioService.PlayAlert(critical);

        Assert.True(playedCritical.HasValue);
        Assert.Equal(critical, playedCritical.Value);
    }

    [Fact]
    public void PlayAlert_WhenPlayerThrowsInvalidOperationException_LogsErrorAndDoesNotThrow()
    {
        AudioService.SetSoundPlayerForTesting(_ => throw new InvalidOperationException("Audio device busy"));
        AudioService.Enabled = true;

        var ex = Record.Exception(() => AudioService.PlayAlert(true));

        Assert.Null(ex);

        Logger.FlushForTesting();
        string logFile = Path.Combine(_testLogDir, "noderadar_system.log");
        Assert.True(File.Exists(logFile));
        string content = File.ReadAllText(logFile);
        Assert.Contains("AudioService", content);
        Assert.Contains("Failed to play sound alert. Audio device busy", content);
    }

    [Fact]
    public void PlayAlert_WhenPlayerThrowsPlatformNotSupportedException_LogsErrorAndDoesNotThrow()
    {
        AudioService.SetSoundPlayerForTesting(_ => throw new PlatformNotSupportedException("Audio not supported on platform"));
        AudioService.Enabled = true;

        var ex = Record.Exception(() => AudioService.PlayAlert(false));

        Assert.Null(ex);

        Logger.FlushForTesting();
        string logFile = Path.Combine(_testLogDir, "noderadar_system.log");
        Assert.True(File.Exists(logFile));
        string content = File.ReadAllText(logFile);
        Assert.Contains("AudioService", content);
        Assert.Contains("Failed to play sound alert. Audio not supported on platform", content);
    }

    [Fact]
    public void PlayAlert_WhenEnabledWithDefaultPlayer_HandlesExceptionsGracefully()
    {
        AudioService.SetSoundPlayerForTesting(null);
        AudioService.Enabled = true;

        var ex1 = Record.Exception(() => AudioService.PlayAlert(true));
        var ex2 = Record.Exception(() => AudioService.PlayAlert(false));

        Assert.Null(ex1);
        Assert.Null(ex2);
    }
}
