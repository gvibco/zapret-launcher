namespace ZapretLauncher.Core.Runtime;

public sealed class LogService
{
    private readonly object _gate = new();
    private readonly string _logDir;

    public LogService(string dataRoot)
    {
        _logDir = Path.Combine(dataRoot, "logs");
        Directory.CreateDirectory(_logDir);
    }

    public event Action<string>? LineWritten;

    public string AppLogPath => Path.Combine(_logDir, $"app-{DateTime.Now:yyyyMMdd}.log");
    public string BypassLogPath => Path.Combine(_logDir, "winws.log");

    public void Write(string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss} {message}";
        lock (_gate) File.AppendAllText(AppLogPath, line + Environment.NewLine);
        LineWritten?.Invoke(line);
    }

    public void WriteBypass(string line)
    {
        lock (_gate) File.AppendAllText(BypassLogPath, line + Environment.NewLine);
        LineWritten?.Invoke(line);
    }
}
