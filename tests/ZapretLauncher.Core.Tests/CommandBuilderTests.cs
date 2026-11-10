using ZapretLauncher.Core.Models;
using ZapretLauncher.Core.Strategies;

namespace ZapretLauncher.Core.Tests;

public class CommandBuilderTests
{
    private static readonly BuildContext Context = new(
        EngineDir: @"C:\data\engine\1.10.4",
        ListsDir: @"C:\data\data\lists",
        GameTcp: "12",
        GameUdp: "12");

    private static Strategy LoadAlt11()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "assets", "strategies.json");
        return StrategyJson.Deserialize(File.ReadAllText(path)).Single(s => s.Id == "alt11");
    }

    [Fact]
    public void Build_ExpandsWfAndGamePlaceholders()
    {
        var args = new CommandBuilder().Build(LoadAlt11(), Context);

        Assert.Equal("--wf-tcp=80,443,2053,2083,2087,2096,8443,12", args[0]);
        Assert.Equal("--wf-udp=443,19294-19344,50000-50100,12", args[1]);
    }

    [Fact]
    public void Build_ResolvesBinaryAndListPaths()
    {
        var args = new CommandBuilder().Build(LoadAlt11(), Context);
        var joined = string.Join(" ", args);

        Assert.Contains(@"--dpi-desync-fake-tls=C:\data\engine\1.10.4\bin\stun2.bin", joined);
        Assert.Contains(@"--hostlist=C:\data\data\lists\list-general.txt", joined);
        Assert.Contains(@"--hostlist=C:\data\data\lists\list-general-user.txt", joined);
    }

    [Fact]
    public void Build_KeepsDesyncParameters()
    {
        var args = new CommandBuilder().Build(LoadAlt11(), Context);
        var joined = string.Join(" ", args);

        Assert.Contains("--dpi-desync=fake,multisplit", joined);
        Assert.Contains("--dpi-desync-split-seqovl=664", joined);
        Assert.Contains("--dpi-desync-fooling=ts", joined);
        Assert.Contains("--ip-id=zero", joined);
        Assert.Equal(8, args.Count(a => a == "--new"));
    }
}
