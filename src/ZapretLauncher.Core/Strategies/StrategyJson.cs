using System.Text.Json;
using System.Text.Json.Serialization;
using ZapretLauncher.Core.Models;

namespace ZapretLauncher.Core.Strategies;

public static class StrategyJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(IEnumerable<Strategy> strategies) =>
        JsonSerializer.Serialize(strategies, Options);

    public static List<Strategy> Deserialize(string json) =>
        JsonSerializer.Deserialize<List<Strategy>>(json, Options) ?? new List<Strategy>();
}
