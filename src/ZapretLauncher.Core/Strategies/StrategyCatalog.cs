using System.Reflection;
using ZapretLauncher.Core.Models;

namespace ZapretLauncher.Core.Strategies;

public sealed class StrategyCatalog
{
    private const string ResourceName = "ZapretLauncher.Core.strategies.json";
    private readonly List<Strategy> _strategies;

    public StrategyCatalog(IEnumerable<Strategy> strategies) => _strategies = strategies.ToList();

    public IReadOnlyList<Strategy> All => _strategies;

    public Strategy Get(string id) =>
        _strategies.FirstOrDefault(s => s.Id == id)
        ?? throw new KeyNotFoundException($"Strategy '{id}' not found.");

    public static StrategyCatalog LoadBuiltin()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource {ResourceName} not found.");
        using var reader = new StreamReader(stream);
        return new StrategyCatalog(StrategyJson.Deserialize(reader.ReadToEnd()));
    }

    public void ApplyUserOverrides(string path)
    {
        if (!File.Exists(path)) return;
        var overrides = StrategyJson.Deserialize(File.ReadAllText(path));
        foreach (var item in overrides)
        {
            var index = _strategies.FindIndex(s => s.Id == item.Id);
            if (index >= 0) _strategies[index] = item;
            else _strategies.Add(item);
        }
    }
}
