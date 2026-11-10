using ZapretLauncher.Core.Autostart;
using ZapretLauncher.Core.Diagnostics;

namespace ZapretLauncher.Core.Tests;

public class AutostartServiceTests
{
    [Fact]
    public async Task Enable_UsesLogonTaskWithHighestPrivileges()
    {
        var runner = new FakeRunner();
        var service = new AutostartService(runner);

        await service.EnableAsync(@"C:\Apps\ZapretLauncher.exe");

        Assert.Contains("/SC ONLOGON", runner.LastArguments);
        Assert.Contains("/RL HIGHEST", runner.LastArguments);
        Assert.Contains("ZapretLauncher.exe", runner.LastArguments);
    }

    [Fact]
    public async Task Enable_NonZeroExit_Throws()
    {
        var service = new AutostartService(new FakeRunner { ExitCode = 1 });
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.EnableAsync(@"C:\Apps\ZapretLauncher.exe"));
    }

    private sealed class FakeRunner : IProcessRunner
    {
        public int ExitCode { get; init; }
        public string LastArguments { get; private set; } = "";

        public Task<(int ExitCode, string Output)> RunAsync(
            string fileName, string arguments, CancellationToken ct = default)
        {
            LastArguments = arguments;
            return Task.FromResult((ExitCode, "ok"));
        }
    }
}
