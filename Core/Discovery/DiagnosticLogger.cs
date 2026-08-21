namespace NodeRadarPro.Core.Discovery;

public static class DiagnosticLogger
{
    public static void Log(string tag, string message)
    {
        Logger.Log(LogLevel.Info, tag, message);
    }
}
