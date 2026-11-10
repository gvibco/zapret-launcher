namespace ZapretLauncher.Core.Runtime;

public sealed class LogService : IDisposable
{
    private readonly object _gate = new();
    private readonly string _logDir;
    private StreamWriter? _appWriter;
    private StreamWriter? _bypassWriter;
    private DateOnly _appDate;
    private bool _disposed;

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
        lock (_gate)
        {
            if (_disposed) return;
            var today = DateOnly.FromDateTime(DateTime.Now);
            if (_appDate != today)
            {
                _appWriter?.Dispose();
                _appWriter = null;
                _appDate = today;
            }

            _appWriter ??= Open(AppLogPath);
            _appWriter.WriteLine(line);
        }
        LineWritten?.Invoke(line);
    }

    public void WriteBypass(string line)
    {
        lock (_gate)
        {
            if (_disposed) return;
            _bypassWriter ??= Open(BypassLogPath);
            _bypassWriter.WriteLine(line);
        }
        LineWritten?.Invoke(line);
    }

    private static StreamWriter Open(string path)
    {
        var stream = new FileStream(
            path, FileMode.Append, FileAccess.Write,
            FileShare.ReadWrite | FileShare.Delete, bufferSize: 8192);
        return new StreamWriter(stream) { AutoFlush = true };
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            _appWriter?.Dispose();
            _appWriter = null;
            _bypassWriter?.Dispose();
            _bypassWriter = null;
        }
    }
}
