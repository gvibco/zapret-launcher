using ZapretLauncher.Core.Models;
using ZapretLauncher.Core.Settings;

namespace ZapretLauncher.Core.Tests;

public class SettingsStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "zl-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var settings = new SettingsStore(_root).Load();
        Assert.Equal("none", settings.IpsetMode);
        Assert.False(settings.CustomDomains.Any());
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var store = new SettingsStore(_root);
        var settings = store.Load();
        settings.SelectedStrategyId = "alt11";
        settings.GameFilter.Mode = "tcp";
        settings.CustomDomains.Add(new CustomDomain { Domain = "example.com", Exact = true });
        store.Save(settings);

        var restored = store.Load();
        Assert.Equal("alt11", restored.SelectedStrategyId);
        Assert.Equal("tcp", restored.GameFilter.Mode);
        Assert.True(restored.CustomDomains.Single().Exact);
    }

    [Fact]
    public void Load_CorruptFile_BacksUpAndReturnsDefaults()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "settings.json"), "{ not json");
        var settings = new SettingsStore(_root).Load();

        Assert.Equal("none", settings.IpsetMode);
        Assert.True(File.Exists(Path.Combine(_root, "settings.json.bak")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
