using ZapretLauncher.Core.Diagnostics;

namespace ZapretLauncher.Core.Autostart;

public sealed class AutostartService
{
    public const string TaskName = "ZapretLauncher";
    private readonly IProcessRunner _runner;

    public AutostartService(IProcessRunner runner) => _runner = runner;

    public async Task EnableAsync(string exePath, CancellationToken ct = default)
    {
        var arguments =
            $"/Create /TN \"{TaskName}\" /TR \"\\\"{exePath}\\\"\" /SC ONLOGON /RL HIGHEST /F";
        var result = await _runner.RunAsync("schtasks", arguments, ct);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"schtasks failed: {result.Output}");
    }

    public async Task DisableAsync(CancellationToken ct = default)
    {
        var result = await _runner.RunAsync("schtasks", $"/Delete /TN \"{TaskName}\" /F", ct);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"schtasks failed: {result.Output}");
    }

    public async Task<bool> IsEnabledAsync(CancellationToken ct = default)
    {
        var result = await _runner.RunAsync("schtasks", $"/Query /TN \"{TaskName}\"", ct);
        return result.ExitCode == 0;
    }
}
