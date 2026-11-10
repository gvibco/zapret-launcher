using ZapretLauncher.Core.Models;
using ZapretLauncher.Core.Probing;

namespace ZapretLauncher.Core.Testing;

public interface IDelay
{
    Task WaitAsync(TimeSpan delay, CancellationToken ct = default);
}

public sealed class SystemDelay : IDelay
{
    public Task WaitAsync(TimeSpan delay, CancellationToken ct = default) =>
        Task.Delay(delay, ct);
}

public sealed record StrategyTestResult(
    string StrategyId,
    bool DryRunPassed,
    TestScore Score,
    IReadOnlyList<ProbeResult> Probes,
    string? Error);

public sealed record TestProgress(int Index, int Total, string StrategyName);
