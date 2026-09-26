using Avalonia;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro
{
    internal static class Program
    {
        private const string AppMutexName = "NodeRadarPro_App_Mutex_Active";
        private static Mutex? _mutex;

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Contains("--test") || args.Contains("-t"))
            {
                int exitCode = NodeRadarPro.Core.ScannerDiagnostics.RunDiagnosticsAsync().GetAwaiter().GetResult();
                Environment.Exit(exitCode);
            }

            if (args.Contains("--smoke-test"))
            {
                int exitCode = RunSmokeTest();
                Environment.Exit(exitCode);
            }

            _mutex = new Mutex(true, AppMutexName, out bool createdNew);
            if (!createdNew)
            {
                // Single-instance enforcement: another instance is already running
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                NodeRadarPro.Core.Logger.Log(NodeRadarPro.Core.LogLevel.Error, "CRASH", $"Unhandled Exception: {ex}");

                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PyPie Studio", "NodeRadar Pro", "logs");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "crash.log");
                File.AppendAllText(logPath, $"[{DateTime.Now}] CRITICAL CRASH: {ex}\n");
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                NodeRadarPro.Core.Logger.Log(NodeRadarPro.Core.LogLevel.Error, "TaskError", $"Unobserved Task Exception: {e.Exception}");

                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PyPie Studio", "NodeRadar Pro", "logs");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "crash.log");
                File.AppendAllText(logPath, $"[{DateTime.Now}] UNOBSERVED TASK CRASH: {e.Exception}\n");
                e.SetObserved();
            };

            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                try
                {
                    string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PyPie Studio", "NodeRadar Pro");
                    Directory.CreateDirectory(logDir);
                    File.WriteAllText(Path.Combine(logDir, "crash_log.txt"), ex.ToString());
                }
                catch (Exception writeEx)
                {
                    NodeRadarPro.Core.Logger.Log(NodeRadarPro.Core.LogLevel.Error, "CRASH", $"Failed to write crash log: {writeEx.Message}");
                }
                throw;
            }
            finally
            {
                GC.KeepAlive(_mutex);
            }
        }

        private static int RunSmokeTest()
        {
            try
            {
                Console.WriteLine("Running NodeRadar Pro startup smoke test...");
                var loader = new Avalonia.Platform.StandardAssetLoader(typeof(Program).Assembly);
                string[] criticalAssets =
                [
                    "avares://NodeRadarPro/Resources/NodeRadar Pro Icon.ico",
                    "avares://NodeRadarPro/Resources/NodeRadar Pro Icon.png",
                    "avares://NodeRadarPro/Resources/NodeRadar Pro.png"
                ];

                foreach (var asset in criticalAssets)
                {
                    var uri = new Uri(asset);
                    if (!loader.Exists(uri))
                    {
                        Console.Error.WriteLine($"[CRITICAL] Missing embedded asset: {asset}");
                        return 1;
                    }

                    using var stream = loader.Open(uri);
                    if (stream.Length == 0)
                    {
                        Console.Error.WriteLine($"[CRITICAL] Asset stream empty: {asset}");
                        return 1;
                    }
                }

                Console.WriteLine("All embedded UI assets verified successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CRITICAL] Smoke test failed: {ex}");
                return 1;
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            var builder = AppBuilder.Configure<App>()
                .UsePlatformDetect();
#if DEBUG
            builder = builder.WithDeveloperTools();
#endif
            return builder
                .WithInterFont()
                .LogToTrace();
        }
    }
}
