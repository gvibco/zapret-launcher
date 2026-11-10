using ZapretLauncher.Core.Models;
using ZapretLauncher.Core.Strategies;

namespace ZapretLauncher.Core.Tests;

public class StrategyModelTests
{
    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var strategy = new Strategy
        {
            Id = "alt11",
            Name = "ALT11",
            Wf = new WfSpec { Tcp = "80,443,%GameFilterTCP%", Udp = "443,%GameFilterUDP%" },
            Profiles =
            {
                new StrategyProfile
                {
                    Filter = new FilterSpec
                    {
                        Tcp = "80,443",
                        Hostlist = { "list-general.txt" },
                        HostlistExclude = { "list-exclude.txt" }
                    },
                    Desync = new DesyncSpec
                    {
                        Modes = { "fake", "multisplit" },
                        Repeats = 8,
                        Fooling = { "ts" },
                        SplitPos = { "1" },
                        Seqovl = 664,
                        SeqovlPattern = "bin/tls_clienthello_max_ru.bin",
                        FakeTls = { "bin/stun2.bin", "bin/tls_clienthello_max_ru.bin" },
                        IpId = "zero"
                    }
                }
            }
        };

        var json = StrategyJson.Serialize(new[] { strategy });
        var restored = StrategyJson.Deserialize(json).Single();

        Assert.Equal("alt11", restored.Id);
        Assert.Equal("80,443,%GameFilterTCP%", restored.Wf.Tcp);
        Assert.Single(restored.Profiles);
        Assert.Equal(new[] { "fake", "multisplit" }, restored.Profiles[0].Desync.Modes);
        Assert.Equal(664, restored.Profiles[0].Desync.Seqovl);
        Assert.Equal("zero", restored.Profiles[0].Desync.IpId);
    }
}
