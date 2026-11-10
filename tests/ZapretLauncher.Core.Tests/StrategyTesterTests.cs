using ZapretLauncher.Core.Models;
using ZapretLauncher.Core.Probing;
using ZapretLauncher.Core.Runtime;
using ZapretLauncher.Core.Strategies;
using ZapretLauncher.Core.Testing;

namespace ZapretLauncher.Core.Tests;

public class StrategyTesterTests
{
    private static readonly BuildContext Context =
        new(@"C:\engine", @"C:\lists", "12", "12");

    [Fact]
    public async Task TestAll_PicksStrategyWithMoreCriticalSuccesses()
    {
        var strategies = new[]
        {
            new Strategy { Id = "one", Name = "One", Wf = new WfSpec { Tcp = "1000", Udp = "1000" } },
            new Strategy { Id = "two", Name = "Two", Wf = new WfSpec { Tcp = "2000", Udp = "2000" } }
        };
        var targets = new[]
        {
            new ProbeTarget("YouTube", "https://youtube.test", true),
            new ProbeTarget("Discord", "https://discord.test", true)
        };
        var runner = new FakeRunner();
        var tester = new StrategyTester(runner, new FakeProbe(runner), new CommandBuilder(), new NoDelay());

        var results = await tester.TestAllAsync(strategies, Context, targets);

        var best = StrategyTester.ChooseBest(results);
        Assert.NotNull(best);
        Assert.Equal("one", best!.StrategyId);
    }

    private sealed class FakeRunner : IBypassRunner
    {
        public bool IsRunning { get; private set; }
        public string CurrentArgs { get; private set; } = "";
        public Task StartAsync(string exePath, IReadOnlyList<string> args, CancellationToken ct = default)
        {
            IsRunning = true;
            CurrentArgs = string.Join(" ", args);
            return Task.CompletedTask;
        }
        public Task StopAsync() { IsRunning = false; return Task.CompletedTask; }
        public Task<(int ExitCode, string Output)> DryRunAsync(
            string exePath, IReadOnlyList<string> args, CancellationToken ct = default) =>
            Task.FromResult((0, "ok"));
    }

    private sealed class FakeProbe : IProbe
    {
        private readonly FakeRunner _runner;
        public FakeProbe(FakeRunner runner) => _runner = runner;

        public Task<ProbeResult> ProbeAsync(ProbeTarget target, CancellationToken ct = default)
        {
            var ok = _runner.CurrentArgs.Contains("--wf-tcp=1000")
                     || (target.Name == "YouTube"
                         && _runner.CurrentArgs.Contains("--wf-tcp=2000"));
            return Task.FromResult(new ProbeResult(
                target.Name, target.Url, target.Critical, ok, ok ? 200 : null,
                10, ok ? FailureKind.None : FailureKind.Reset, null));
        }
    }

    private sealed class NoDelay : IDelay
    {
        public Task WaitAsync(TimeSpan delay, CancellationToken ct = default) => Task.CompletedTask;
    }
}
