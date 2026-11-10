using System.Diagnostics;

namespace ZapretLauncher.Core.Runtime;

public enum BypassState
{
    Stopped,
    Running,
    Error
}

public interface IBypassRunner
{
    bool IsRunning { get; }
    Task StartAsync(string exePath, IReadOnlyList<string> args, CancellationToken ct = default);
    Task StopAsync();
    Task<(int ExitCode, string Output)> DryRunAsync(
        string exePath, IReadOnlyList<string> args, CancellationToken ct = default);
}

public sealed class BypassRunner : IBypassRunner, IDisposable
{
    private readonly LogService _log;
    private Process? _process;

    public BypassRunner(LogService log) => _log = log;

    public event EventHandler<BypassState>? StateChanged;

    public bool IsRunning => _process is { HasExited: false };

    public Task StartAsync(string exePath, IReadOnlyList<string> args, CancellationToken ct = default)
    {
        if (IsRunning) throw new InvalidOperationException("Bypass is already running.");

        var psi = CreateStartInfo(exePath, args);
        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) _log.WriteBypass(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) _log.WriteBypass(e.Data); };
        process.Exited += (_, _) =>
        {
            _log.Write($"winws exited with code {process.ExitCode}");
            StateChanged?.Invoke(this, BypassState.Stopped);
        };

        if (!process.Start()) throw new InvalidOperationException("Failed to start winws.exe.");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        _process = process;
        _log.Write("winws start: process started");
        StateChanged?.Invoke(this, BypassState.Running);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        var process = _process;
        if (process is null) return;

        try
        {
            if (!process.HasExited)
            {
                _log.Write("winws stop: killing");
                process.Kill(entireProcessTree: true);
                _log.Write("winws stop: waiting for exit");
                await process.WaitForExitAsync();
                _log.Write("winws stop: exited");
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited.
        }
        finally
        {
            process.Dispose();
            _process = null;
            _log.Write("winws stop: done");
            StateChanged?.Invoke(this, BypassState.Stopped);
        }
    }

    public async Task<(int ExitCode, string Output)> DryRunAsync(
        string exePath, IReadOnlyList<string> args, CancellationToken ct = default)
    {
        var withDryRun = args.Concat(new[] { "--dry-run" }).ToArray();
        using var process = new Process { StartInfo = CreateStartInfo(exePath, withDryRun) };
        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        var output = await outputTask + await errorTask;
        return (process.ExitCode, output);
    }

    private static ProcessStartInfo CreateStartInfo(string exePath, IReadOnlyList<string> args)
    {
        var psi = new ProcessStartInfo(exePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(exePath)!
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);
        return psi;
    }

    public void Dispose()
    {
        if (_process is not null)
        {
            try { _process.Kill(entireProcessTree: true); } catch { }
            _process.Dispose();
            _process = null;
        }
    }
}
