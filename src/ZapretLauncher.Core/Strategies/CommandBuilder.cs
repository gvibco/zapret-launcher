using ZapretLauncher.Core.Models;

namespace ZapretLauncher.Core.Strategies;

public sealed record BuildContext(
    string EngineDir,
    string ListsDir,
    string GameTcp,
    string GameUdp);

public sealed class CommandBuilder
{
    public IReadOnlyList<string> Build(Strategy strategy, BuildContext context)
    {
        var args = new List<string>
        {
            "--wf-tcp=" + Expand(strategy.Wf.Tcp, context),
            "--wf-udp=" + Expand(strategy.Wf.Udp, context)
        };

        for (var i = 0; i < strategy.Profiles.Count; i++)
        {
            if (i > 0) args.Add("--new");
            AddProfile(args, strategy.Profiles[i], context);
        }

        return args;
    }

    private static string Expand(string value, BuildContext context)
    {
        var expanded = value
            .Replace("%GameFilterTCP%", context.GameTcp, StringComparison.OrdinalIgnoreCase)
            .Replace("%GameFilterUDP%", context.GameUdp, StringComparison.OrdinalIgnoreCase);
        return string.Join(",", expanded.Split(',', StringSplitOptions.RemoveEmptyEntries));
    }

    private static void AddProfile(List<string> args, StrategyProfile profile, BuildContext context)
    {
        var filter = profile.Filter;
        Add(args, "--filter-tcp", filter.Tcp is null ? null : Expand(filter.Tcp, context));
        Add(args, "--filter-udp", filter.Udp is null ? null : Expand(filter.Udp, context));
        Add(args, "--filter-l3", filter.L3);
        Add(args, "--filter-l7", filter.L7);

        foreach (var item in filter.Hostlist)
            args.Add("--hostlist=" + ResolveList(item, context));
        var hasGeneral = filter.Hostlist.Any(h =>
            Path.GetFileName(h).Equals("list-general.txt", StringComparison.OrdinalIgnoreCase));
        var hasGeneralUser = filter.Hostlist.Any(h =>
            Path.GetFileName(h).Equals("list-general-user.txt", StringComparison.OrdinalIgnoreCase));
        if (hasGeneral && !hasGeneralUser)
            args.Add("--hostlist=" + ResolveList("list-general-user.txt", context));

        AddJoined(args, "--hostlist-domains", filter.HostlistDomains);
        foreach (var item in filter.HostlistExclude)
            args.Add("--hostlist-exclude=" + ResolveList(item, context));
        AddJoined(args, "--hostlist-exclude-domains", filter.HostlistExcludeDomains);
        foreach (var item in filter.Ipset)
            args.Add("--ipset=" + ResolveList(item, context));
        foreach (var item in filter.IpsetExclude)
            args.Add("--ipset-exclude=" + ResolveList(item, context));

        var desync = profile.Desync;
        AddJoined(args, "--dpi-desync", desync.Modes);
        if (desync.AnyProtocol) args.Add("--dpi-desync-any-protocol=1");
        Add(args, "--dpi-desync-badseq-increment", desync.BadseqIncrement?.ToString());
        Add(args, "--dpi-desync-cutoff", desync.Cutoff);
        foreach (var item in desync.FakeDiscord)
            args.Add("--dpi-desync-fake-discord=" + ResolveBinary(item, context));
        Add(args, "--dpi-desync-fakedsplit-pattern", desync.FakedsplitPattern);
        foreach (var item in desync.FakeHttp)
            args.Add("--dpi-desync-fake-http=" + ResolveBinary(item, context));
        foreach (var item in desync.FakeQuic)
            args.Add("--dpi-desync-fake-quic=" + ResolveBinary(item, context));
        foreach (var item in desync.FakeStun)
            args.Add("--dpi-desync-fake-stun=" + ResolveBinary(item, context));
        foreach (var item in desync.FakeTls)
            args.Add("--dpi-desync-fake-tls=" + ResolveBinary(item, context));
        Add(args, "--dpi-desync-fake-tls-mod", desync.FakeTlsMod);
        foreach (var item in desync.FakeUnknown)
            args.Add("--dpi-desync-fake-unknown=" + ResolveBinary(item, context));
        foreach (var item in desync.FakeUnknownUdp)
            args.Add("--dpi-desync-fake-unknown-udp=" + ResolveBinary(item, context));
        AddJoined(args, "--dpi-desync-fooling", desync.Fooling);
        Add(args, "--dpi-desync-hostfakesplit-mod", desync.HostfakesplitMod);
        Add(args, "--dpi-desync-repeats", desync.Repeats?.ToString());
        AddJoined(args, "--dpi-desync-split-pos", desync.SplitPos);
        Add(args, "--dpi-desync-split-seqovl", desync.Seqovl?.ToString());
        Add(args, "--dpi-desync-split-seqovl-pattern", desync.SeqovlPattern is null ? null : ResolveBinary(desync.SeqovlPattern, context));
        Add(args, "--ip-id", desync.IpId);

        args.AddRange(profile.ExtraArgs);
    }

    private static void Add(List<string> args, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) args.Add($"{name}={value}");
    }

    private static void AddJoined(List<string> args, string name, IReadOnlyCollection<string> values)
    {
        if (values.Count > 0) args.Add($"{name}={string.Join(",", values)}");
    }

    private static string ResolveList(string value, BuildContext context) =>
        Path.IsPathRooted(value) ? value : Path.Combine(context.ListsDir, value);

    private static string ResolveBinary(string value, BuildContext context)
    {
        if (value.StartsWith("bin/", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(context.EngineDir, value.Replace('/', Path.DirectorySeparatorChar));
        return value;
    }
}
