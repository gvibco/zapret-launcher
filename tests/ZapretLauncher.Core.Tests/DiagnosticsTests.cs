using ZapretLauncher.Core.Diagnostics;

namespace ZapretLauncher.Core.Tests;

public class DiagnosticsTests
{
    [Fact]
    public void TimestampsParser_DetectsEnabled()
    {
        const string output = "TCP Global Parameters\n----------------------------------------------\nTimestamps                         enabled\n";
        Assert.True(DiagnosticsService.TimestampsEnabled(output));
    }

    [Fact]
    public void TimestampsParser_DetectsDisabled()
    {
        const string output = "Timestamps                         disabled\n";
        Assert.False(DiagnosticsService.TimestampsEnabled(output));
    }

    [Fact]
    public void HostsCheck_DetectsYoutubeEntries()
    {
        const string hosts = "127.0.0.1 localhost\n142.250.0.1 youtube.com\n";
        Assert.True(DiagnosticsService.HasYoutubeHosts(hosts));
        Assert.False(DiagnosticsService.HasYoutubeHosts("127.0.0.1 localhost\n"));
    }
}
