using ZapretLauncher.Core.Models;
using ZapretLauncher.Core.Probing;
using ZapretLauncher.Core.Runtime;
using ZapretLauncher.Core.Strategies;

namespace ZapretLauncher.Core.Testing;

public sealed class StrategyTester
{
    private readonly IBypassRunner _runner;
    private readonly IProbe _probe;
    private readonly CommandBuilder _builder;
    private readonly IDelay _delay;

    public StrategyTester(
        IBypassRunner runner, IProbe probe, CommandBuilder builder, IDelay delay)
    {
        _runner = runner;
        _probe = probe;
        _builder = builder;
        _delay = delay;
    }

    public async Task<IReadOnlyList<StrategyTestResult>> TestAllAsync(
        IEnumerable<Strategy> strategies,
        BuildContext context,
        IReadOnlyList<ProbeTarget> targets,
        IProgress<TestProgress>? progress = null,
        CancellationToken ct = default)
    {
        var list = strategies.ToList();
        var results = new List<StrategyTestResult>(list.Count);
        var exe = Path.Combine(context.EngineDir, "winws.exe");

        for (var index = 0; index < list.Count; index++)
        {
            ct.ThrowIfCancellationRequested();
            var strategy = list[index];
            progress?.Report(new TestProgress(index + 1, list.Count, strategy.Name));
            var args = _builder.Build(strategy, context);
            var dry = await _runner.DryRunAsync(exe, args, ct);
            if (dry.ExitCode != 0)
            {
                results.Add(new StrategyTestResult(
                    strategy.Id, false, new TestScore(0, 0, long.MaxValue),
                    Array.Empty<ProbeResult>(), dry.Output));
                continue;
            }

            IReadOnlyList<ProbeResult> probes;
            try
            {
                await _runner.StartAsync(exe, args, ct);
                await _delay.WaitAsync(TimeSpan.FromSeconds(2), ct);
                var tasks = targets.Select(t => _probe.ProbeAsync(t, ct));
                probes = await Task.WhenAll(tasks);
            }
            finally
            {
                await _runner.StopAsync();
                await _delay.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None);
            }

            results.Add(new StrategyTestResult(
                strategy.Id, true, Score(probes), probes, null));
        }

        return results;
    }

    public static TestScore Score(IReadOnlyList<ProbeResult> probes)
    {
        var successful = probes.Where(p => p.Success).ToList();
        var critical = successful.Count(p => p.Critical);
        var median = 0L;
        if (successful.Count > 0)
        {
            var ordered = successful.Select(p => p.ElapsedMs).OrderBy(ms => ms).ToArray();
            median = ordered.Length % 2 == 1
                ? ordered[ordered.Length / 2]
                : (ordered[ordered.Length / 2 - 1] + ordered[ordered.Length / 2]) / 2;
        }
        else
        {
            median = long.MaxValue;
        }
        return new TestScore(critical, successful.Count, median);
    }

    public static StrategyTestResult? ChooseBest(IEnumerable<StrategyTestResult> results) =>
        results
            .Where(r => r.DryRunPassed)
            .OrderByDescending(r => r.Score.CriticalPassed)
            .ThenByDescending(r => r.Score.TotalPassed)
            .ThenBy(r => r.Score.MedianMs)
            .FirstOrDefault();
}
