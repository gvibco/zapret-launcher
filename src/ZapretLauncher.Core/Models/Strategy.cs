namespace ZapretLauncher.Core.Models;

public sealed class Strategy
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Source { get; set; } = "builtin";
    public WfSpec Wf { get; set; } = new();
    public List<StrategyProfile> Profiles { get; set; } = new();
}

public sealed class WfSpec
{
    public string Tcp { get; set; } = "";
    public string Udp { get; set; } = "";
}

public sealed class StrategyProfile
{
    public FilterSpec Filter { get; set; } = new();
    public DesyncSpec Desync { get; set; } = new();
    public List<string> ExtraArgs { get; set; } = new();
}

public sealed class FilterSpec
{
    public string? Tcp { get; set; }
    public string? Udp { get; set; }
    public string? L3 { get; set; }
    public string? L7 { get; set; }
    public List<string> Hostlist { get; set; } = new();
    public List<string> HostlistDomains { get; set; } = new();
    public List<string> HostlistExclude { get; set; } = new();
    public List<string> HostlistExcludeDomains { get; set; } = new();
    public List<string> Ipset { get; set; } = new();
    public List<string> IpsetExclude { get; set; } = new();
}

public sealed class DesyncSpec
{
    public List<string> Modes { get; set; } = new();
    public bool AnyProtocol { get; set; }
    public int? BadseqIncrement { get; set; }
    public string? Cutoff { get; set; }
    public List<string> FakeDiscord { get; set; } = new();
    public string? FakedsplitPattern { get; set; }
    public List<string> FakeHttp { get; set; } = new();
    public List<string> FakeQuic { get; set; } = new();
    public List<string> FakeStun { get; set; } = new();
    public List<string> FakeTls { get; set; } = new();
    public string? FakeTlsMod { get; set; }
    public List<string> FakeUnknown { get; set; } = new();
    public List<string> FakeUnknownUdp { get; set; } = new();
    public List<string> Fooling { get; set; } = new();
    public string? HostfakesplitMod { get; set; }
    public int? Repeats { get; set; }
    public List<string> SplitPos { get; set; } = new();
    public int? Seqovl { get; set; }
    public string? SeqovlPattern { get; set; }
    public string? IpId { get; set; }
}
