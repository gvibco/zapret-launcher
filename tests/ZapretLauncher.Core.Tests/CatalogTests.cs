using ZapretLauncher.Core.Strategies;

namespace ZapretLauncher.Core.Tests;

public class CatalogTests
{
    [Fact]
    public void BuiltinCatalog_Has22Strategies_AndAlt11IsComplete()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "assets", "strategies.json");
        var strategies = StrategyJson.Deserialize(File.ReadAllText(path));

        Assert.Equal(22, strategies.Count);
        var alt11 = strategies.Single(s => s.Id == "alt11");
        Assert.Equal(9, alt11.Profiles.Count);
        Assert.Contains("fake", alt11.Profiles[0].Desync.Modes);
        Assert.Contains(alt11.Profiles, p => p.Desync.IpId == "zero");
        Assert.Contains(alt11.Profiles, p => p.Desync.Seqovl == 664);
    }

    [Fact]
    public void LoadBuiltin_UsesEmbeddedResource()
    {
        Assert.Equal(22, StrategyCatalog.LoadBuiltin().All.Count);
    }
}
