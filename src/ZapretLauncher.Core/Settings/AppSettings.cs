using ZapretLauncher.Core.Models;

namespace ZapretLauncher.Core.Settings;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public string? SelectedStrategyId { get; set; }
    public Dictionary<string, NetworkResult> LastGoodByNetwork { get; set; } = new();
    public string IpsetMode { get; set; } = "none";
    public GameFilterSettings GameFilter { get; set; } = new();
    public bool Autostart { get; set; } = true;
    public bool StartMinimized { get; set; } = true;
    public bool CloseToTray { get; set; } = true;
    public string ListBaseUrl { get; set; } =
        "https://raw.githubusercontent.com/Flowseal/zapret-discord-youtube/main/";
    public List<CustomDomain> CustomDomains { get; set; } = new();
    public List<string> CustomExclusions { get; set; } = new();
    public List<string> CustomIps { get; set; } = new();
    public List<string> Targets { get; set; } = new();
    public DateTimeOffset? LastUpdateCheck { get; set; }
}

public sealed class GameFilterSettings
{
    public string Mode { get; set; } = "disabled";
    public string TcpRange { get; set; } = "1024-65535";
    public string UdpRange { get; set; } = "1024-65535";
}

public sealed class CustomDomain
{
    public string Domain { get; set; } = "";
    public bool Exact { get; set; }
}

public sealed class NetworkResult
{
    public string StrategyId { get; set; } = "";
    public TestScore Score { get; set; } = new(0, 0, long.MaxValue);
    public DateTimeOffset TestedAt { get; set; }
    public string EngineVersion { get; set; } = "";
    public string IpsetMode { get; set; } = "none";
    public string GameFilterMode { get; set; } = "disabled";
}
