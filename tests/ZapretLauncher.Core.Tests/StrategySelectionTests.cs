using ZapretLauncher.Core.Models;
using ZapretLauncher.Core.Strategies;

namespace ZapretLauncher.Core.Tests;

public class StrategySelectionTests
{
    private static IReadOnlyList<Strategy> Catalog() => new[]
    {
        new Strategy { Id = "first", Name = "First" },
        new Strategy { Id = "last-good", Name = "Last good" },
        new Strategy { Id = "manual", Name = "Manual" }
    };

    [Fact]
    public void ManualChoice_WinsOverLastGood()
    {
        var selected = StrategySelection.Resolve(Catalog(), "manual", "last-good");

        Assert.Equal("manual", selected!.Id);
    }

    [Fact]
    public void InvalidManualChoice_FallsBackToLastGood()
    {
        var selected = StrategySelection.Resolve(Catalog(), "missing", "last-good");

        Assert.Equal("last-good", selected!.Id);
    }

    [Fact]
    public void MissingBoth_FallsBackToFirst()
    {
        var selected = StrategySelection.Resolve(Catalog(), null, "missing");

        Assert.Equal("first", selected!.Id);
    }
}
