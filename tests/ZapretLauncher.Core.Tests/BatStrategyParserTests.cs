using ZapretLauncher.Core.Strategies;

namespace ZapretLauncher.Core.Tests;

public class BatStrategyParserTests
{
    private const string Bat = """
        @echo off
        start "zapret: %~n0" /min "%BIN%winws.exe" --wf-tcp=80,443,%GameFilterTCP% --wf-udp=443,%GameFilterUDP% ^
        --filter-tcp=80,443 --hostlist="%LISTS%list-general.txt" --dpi-desync=fake,multisplit --dpi-desync-split-pos=1 ^
        --dpi-desync-fake-tls="%BIN%stun2.bin" --ip-id=zero --new ^
        --filter-l7=discord,stun --dpi-desync=fake --dpi-desync-repeats=6
        """;

    [Fact]
    public void Parse_ExtractsWfProfilesAndValues()
    {
        var strategy = BatStrategyParser.Parse(Bat, "general (ALT11).bat");

        Assert.Equal("alt11", strategy.Id);
        Assert.Equal("ALT11", strategy.Name);
        Assert.Equal("80,443,%GameFilterTCP%", strategy.Wf.Tcp);
        Assert.Equal("443,%GameFilterUDP%", strategy.Wf.Udp);
        Assert.Equal(2, strategy.Profiles.Count);

        var first = strategy.Profiles[0];
        Assert.Equal("80,443", first.Filter.Tcp);
        Assert.Equal(new[] { "list-general.txt" }, first.Filter.Hostlist);
        Assert.Equal(new[] { "fake", "multisplit" }, first.Desync.Modes);
        Assert.Equal(new[] { "1" }, first.Desync.SplitPos);
        Assert.Equal(new[] { "bin/stun2.bin" }, first.Desync.FakeTls);
        Assert.Equal("zero", first.Desync.IpId);

        var second = strategy.Profiles[1];
        Assert.Equal("discord,stun", second.Filter.L7);
        Assert.Equal(6, second.Desync.Repeats);
    }
}
