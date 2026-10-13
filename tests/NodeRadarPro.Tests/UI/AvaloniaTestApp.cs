using Avalonia;

namespace NodeRadarPro.Tests.UI;

internal static class AvaloniaTestApp
{
    private static readonly object SyncRoot = new();
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                AppBuilder.Configure<Avalonia.Application>()
                    .UsePlatformDetect()
                    .WithInterFont()
                    .SetupWithoutStarting();
            }
            catch
            {
                // Avalonia platform may already be initialized by preceding test fixture
            }

            _initialized = true;
        }
    }
}
