using System;
using System.IO;

namespace BlueGlassMihomoClient.Services;

public static class LogService
{
    private static readonly string AppLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "app.log");
    private static readonly string MihomoLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "mihomo.log");

    static LogService()
    {
        try
        {
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs"));
        }
        catch { }
    }

    public static void LogApp(string message)
    {
        try
        {
            File.AppendAllText(AppLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch { }
    }

    public static void LogMihomo(string message)
    {
        try
        {
            File.AppendAllText(MihomoLogPath, message + Environment.NewLine);
        }
        catch { }
    }
}
