using ZapretLauncher.Core.Runtime;

namespace ZapretLauncher.Core.Tests;

public class BypassRunnerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "zl-" + Guid.NewGuid().ToString("N"));
    private readonly string _cmd = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";

    [Fact]
    public async Task Start_And_Stop_TracksProcess()
    {
        var runner = new BypassRunner(new LogService(_root));

        await runner.StartAsync(_cmd, new[] { "/c", "ping", "-n", "30", "127.0.0.1" });
        await Task.Delay(300);
        Assert.True(runner.IsRunning);

        await runner.StopAsync();
        Assert.False(runner.IsRunning);
    }

    [Fact]
    public async Task DryRun_ReturnsExitCodeAndOutput()
    {
        var runner = new BypassRunner(new LogService(_root));
        var result = await runner.DryRunAsync(_cmd, new[] { "/c", "echo", "ok" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("ok", result.Output);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
