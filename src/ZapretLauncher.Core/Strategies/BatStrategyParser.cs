using System.Text;
using System.Text.RegularExpressions;
using ZapretLauncher.Core.Models;

namespace ZapretLauncher.Core.Strategies;

public static partial class BatStrategyParser
{
    [GeneratedRegex("\"[^\"]*\"|\\S+")]
    private static partial Regex TokenRegex();

    public static Strategy Parse(string batText, string fileName)
    {
        var name = NameFromFile(fileName);
        var id = IdFromName(name);
        var joined = JoinContinuationLines(batText);
        var line = joined.Split('\n')
            .FirstOrDefault(l => l.Contains("winws.exe", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"winws.exe not found in {fileName}");

        var index = line.IndexOf("winws.exe", StringComparison.OrdinalIgnoreCase);
        var argText = line[(index + "winws.exe".Length)..].TrimStart('"', ' ');
        var tokens = TokenRegex().Matches(argText).Select(m => m.Value).ToList();

        string? wfTcp = null;
        string? wfUdp = null;
        var profiles = new List<StrategyProfile>();
        ProfileBuilder? current = null;

        foreach (var token in tokens)
        {
            if (token == "--new")
            {
                if (current is not null) profiles.Add(current.Build());
                current = new ProfileBuilder();
                continue;
            }

            var match = Regex.Match(token, "^--(?<key>[a-z0-9-]+)=(?<value>.*)$");
            if (!match.Success) continue;

            var key = match.Groups["key"].Value;
            var value = Normalize(match.Groups["value"].Value);

            switch (key)
            {
                case "wf-tcp": wfTcp = value; continue;
                case "wf-udp": wfUdp = value; continue;
            }

            current ??= new ProfileBuilder();
            current.Add(key, value);
        }

        if (current is not null) profiles.Add(current.Build());

        return new Strategy
        {
            Id = id,
            Name = name,
            Source = "builtin",
            Wf = new WfSpec { Tcp = wfTcp ?? "", Udp = wfUdp ?? "" },
            Profiles = profiles
        };
    }

    private static string JoinContinuationLines(string text)
    {
        var builder = new StringBuilder();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.EndsWith('^')) builder.Append(line[..^1]).Append(' ');
            else builder.Append(line).Append('\n');
        }
        return builder.ToString();
    }

    private static string Normalize(string value)
    {
        var result = value.Trim();
        if (result.Length >= 2 && result[0] == '"' && result[^1] == '"')
            result = result[1..^1];

        return result
            .Replace("%BIN%", "bin/", StringComparison.OrdinalIgnoreCase)
            .Replace("%LISTS%", "", StringComparison.OrdinalIgnoreCase)
            .Replace("^!", "!", StringComparison.Ordinal)
            .Replace('\\', '/');
    }

    private static string NameFromFile(string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        if (baseName.Equals("general", StringComparison.OrdinalIgnoreCase)) return "general";
        var match = Regex.Match(baseName, @"^general \((?<name>.+)\)$");
        return match.Success ? match.Groups["name"].Value : baseName;
    }

    private static string IdFromName(string name)
    {
        var id = Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(id) ? "strategy" : id;
    }

    private sealed class ProfileBuilder
    {
        private readonly FilterSpec _filter = new();
        private readonly DesyncSpec _desync = new();
        private readonly List<string> _extra = new();

        public void Add(string key, string value)
        {
            switch (key)
            {
                case "filter-tcp": _filter.Tcp = value; break;
                case "filter-udp": _filter.Udp = value; break;
                case "filter-l3": _filter.L3 = value; break;
                case "filter-l7": _filter.L7 = value; break;
                case "hostlist": _filter.Hostlist.Add(value); break;
                case "hostlist-domains": _filter.HostlistDomains.Add(value); break;
                case "hostlist-exclude": _filter.HostlistExclude.Add(value); break;
                case "hostlist-exclude-domains": _filter.HostlistExcludeDomains.Add(value); break;
                case "ipset": _filter.Ipset.Add(value); break;
                case "ipset-exclude": _filter.IpsetExclude.Add(value); break;

                case "dpi-desync": _desync.Modes.AddRange(Split(value)); break;
                case "dpi-desync-any-protocol": _desync.AnyProtocol = value == "1"; break;
                case "dpi-desync-badseq-increment": _desync.BadseqIncrement = ParseInt(value); break;
                case "dpi-desync-cutoff": _desync.Cutoff = value; break;
                case "dpi-desync-fake-discord": _desync.FakeDiscord.Add(value); break;
                case "dpi-desync-fakedsplit-pattern": _desync.FakedsplitPattern = value; break;
                case "dpi-desync-fake-http": _desync.FakeHttp.Add(value); break;
                case "dpi-desync-fake-quic": _desync.FakeQuic.Add(value); break;
                case "dpi-desync-fake-stun": _desync.FakeStun.Add(value); break;
                case "dpi-desync-fake-tls": _desync.FakeTls.Add(value); break;
                case "dpi-desync-fake-tls-mod": _desync.FakeTlsMod = value; break;
                case "dpi-desync-fake-unknown": _desync.FakeUnknown.Add(value); break;
                case "dpi-desync-fake-unknown-udp": _desync.FakeUnknownUdp.Add(value); break;
                case "dpi-desync-fooling": _desync.Fooling.AddRange(Split(value)); break;
                case "dpi-desync-hostfakesplit-mod": _desync.HostfakesplitMod = value; break;
                case "dpi-desync-repeats": _desync.Repeats = ParseInt(value); break;
                case "dpi-desync-split-pos": _desync.SplitPos.AddRange(Split(value)); break;
                case "dpi-desync-split-seqovl": _desync.Seqovl = ParseInt(value); break;
                case "dpi-desync-split-seqovl-pattern": _desync.SeqovlPattern = value; break;
                case "ip-id": _desync.IpId = value; break;

                default: _extra.Add($"--{key}={value}"); break;
            }
        }

        public StrategyProfile Build() => new()
        {
            Filter = _filter,
            Desync = _desync,
            ExtraArgs = _extra
        };

        private static IEnumerable<string> Split(string value) =>
            value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        private static int? ParseInt(string value) =>
            int.TryParse(value, out var number) ? number : null;
    }
}
