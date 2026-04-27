using Avalonia;
using System;
using Velopack;

namespace NodeRadar_Pro
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) 
        {
            VelopackApp.Build().Run();
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                var ex = e.ExceptionObject as Exception;
                string logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PyPie Studio", "NodeRadar Pro", "logs");
                System.IO.Directory.CreateDirectory(logDir);
                string logPath = System.IO.Path.Combine(logDir, "crash.log");
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] CRITICAL CRASH: {ex}\n");
            };

            try 
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("crash_log.txt", ex.ToString());
                throw;
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
#if DEBUG
                .WithDeveloperTools()
#endif
                .WithInterFont()
                .LogToTrace();
    }
}
