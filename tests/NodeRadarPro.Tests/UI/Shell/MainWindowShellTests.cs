namespace NodeRadarPro.Tests.UI.Shell;

[Collection("AvaloniaUIStaticState")]
public class MainWindowShellTests
{
    public MainWindowShellTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void InitializeAutoBackupTimer_WithDisabledSetting_DoesNotThrow()
    {
        var settings = new AppSettings { EnableAutoBackup = false };
        var exception = Record.Exception(() => MainWindowShell.InitializeAutoBackupTimer(settings));
        Assert.Null(exception);
    }

    [Fact]
    public void InitializeAutoBackupTimer_WithEnabledSetting_DoesNotThrow()
    {
        var settings = new AppSettings { EnableAutoBackup = true, AutoBackupIntervalHours = 12 };
        var exception = Record.Exception(() => MainWindowShell.InitializeAutoBackupTimer(settings));
        Assert.Null(exception);
    }
}
