using ZapretLauncher.Core.Models;

namespace ZapretLauncher.Core.Strategies;

public static class StrategySelection
{
    /// <summary>
    /// A manual choice wins over the remembered last-good strategy for the network.
    /// The last-good entry is only a fallback when the saved selection is missing.
    /// </summary>
    public static Strategy? Resolve(
        IReadOnlyList<Strategy> all,
        string? selectedId,
        string? lastGoodId)
    {
        var selected = all.FirstOrDefault(s => s.Id == selectedId);
        if (selected is not null) return selected;
        return all.FirstOrDefault(s => s.Id == lastGoodId) ?? all.FirstOrDefault();
    }
}
