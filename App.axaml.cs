using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using NodeRadarPro.UI;
using NodeRadarPro.Data;
using NodeRadarPro.Core;

namespace NodeRadar_Pro
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            // We strip out XAML loading and insert our pure C# styles
            Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
            RequestedThemeVariant = ThemeVariant.Dark;
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Inject our PyPie Studio Dark Theme instead of the XAML MainWindow
                desktop.MainWindow = DarkPurpleTheme.BuildMainWindow();
                DarkPurpleTheme.OnStartup(desktop.MainWindow);
            }

            try
            {
                var db = LocalDatabase.Instance;
            }
            catch (System.Exception ex)
            {
                Logger.Log(LogLevel.Error, "Database", $"Initialization failed: {ex.Message}");
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
