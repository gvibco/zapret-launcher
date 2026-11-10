using ZapretLauncher.Core.Runtime;
using ZapretLauncher.Core.Settings;

namespace ZapretLauncher.Core.Coverage;

public sealed class CoverageService
{
    private static readonly (string Remote, string Local)[] DownloadedFiles =
    {
        ("lists/list-general.txt", "list-general.txt"),
        ("lists/list-google.txt", "list-google.txt"),
        ("lists/list-exclude.txt", "list-exclude.txt"),
        ("lists/ipset-exclude.txt", "ipset-exclude.txt"),
        (".service/ipset-service.txt", "ipset-base.txt")
    };

    private static readonly string[] BaseFiles =
    {
        "list-general.txt", "list-google.txt", "list-exclude.txt", "ipset-exclude.txt"
    };

    private readonly HttpClient _http;
    private readonly LogService _log;

    public CoverageService(string dataRoot, HttpClient http, LogService log)
    {
        ListsDir = Path.Combine(dataRoot, "data", "lists");
        Directory.CreateDirectory(ListsDir);
        _http = http;
        _log = log;
    }

    public string ListsDir { get; }

    public void EnsureDefaults(string engineDir)
    {
        var engineLists = Path.Combine(engineDir, "lists");
        if (Directory.Exists(engineLists))
        {
            foreach (var file in BaseFiles)
            {
                var source = Path.Combine(engineLists, file);
                var target = Path.Combine(ListsDir, file);
                if (File.Exists(source) && !File.Exists(target))
                    File.Copy(source, target);
            }
        }

        EnsureUserFile("list-general-user.txt", "domain.example.abc");
        EnsureUserFile("list-exclude-user.txt", "domain.example.abc");
        EnsureUserFile("ipset-exclude-user.txt", "203.0.113.113/32");
        EnsureUserFile("ipset-user.txt", "");
    }

    public async Task<bool> UpdateListsAsync(string baseUrl, CancellationToken ct = default)
    {
        var allOk = true;
        foreach (var (remote, local) in DownloadedFiles)
        {
            try
            {
                var url = baseUrl.TrimEnd('/') + "/" + remote;
                var text = await _http.GetStringAsync(url, ct);
                if (!IsValidList(text))
                {
                    _log.Write($"List rejected: {remote}");
                    allOk = false;
                    continue;
                }
                WriteAtomic(Path.Combine(ListsDir, local), text);
                _log.Write($"List updated: {local}");
            }
            catch (Exception ex)
            {
                _log.Write($"List update failed: {remote}: {ex.Message}");
                allOk = false;
            }
        }
        return allOk;
    }

    public void Apply(AppSettings settings)
    {
        Directory.CreateDirectory(ListsDir);

        var domains = settings.CustomDomains
            .Select(d => (Domain: NormalizeDomain(d.Domain), d.Exact))
            .Where(x => x.Domain.Length > 0)
            .Select(x => x.Exact ? "^" + x.Domain : x.Domain)
            .DefaultIfEmpty("domain.example.abc");
        WriteAtomic(Path.Combine(ListsDir, "list-general-user.txt"),
            string.Join(Environment.NewLine, domains) + Environment.NewLine);

        var exclusions = settings.CustomExclusions
            .Select(NormalizeDomain)
            .Where(d => d.Length > 0)
            .DefaultIfEmpty("domain.example.abc");
        WriteAtomic(Path.Combine(ListsDir, "list-exclude-user.txt"),
            string.Join(Environment.NewLine, exclusions) + Environment.NewLine);

        var userIps = settings.CustomIps
            .Select(ip => ip.Trim())
            .Where(ip => ip.Length > 0)
            .Distinct()
            .ToList();
        WriteAtomic(Path.Combine(ListsDir, "ipset-user.txt"),
            string.Join(Environment.NewLine, userIps) + Environment.NewLine);

        var ipset = settings.IpsetMode switch
        {
            "any" => "",
            "loaded" => string.Join(Environment.NewLine,
                ReadLines("ipset-base.txt").Concat(userIps).Distinct()),
            _ => "203.0.113.113/32"
        };
        WriteAtomic(Path.Combine(ListsDir, "ipset-all.txt"),
            ipset + (ipset.Length > 0 ? Environment.NewLine : ""));
    }

    public static (string Tcp, string Udp) GameValues(GameFilterSettings settings) =>
        settings.Mode switch
        {
            "tcp" => (settings.TcpRange, "12"),
            "udp" => ("12", settings.UdpRange),
            "all" => (settings.TcpRange, settings.UdpRange),
            _ => ("12", "12")
        };

    private static bool IsValidList(string text) =>
        text.Length > 100 &&
        !text.TrimStart().StartsWith("<", StringComparison.Ordinal);

    private static string NormalizeDomain(string raw)
    {
        var value = raw.Trim().ToLowerInvariant();
        var scheme = value.IndexOf("://", StringComparison.Ordinal);
        if (scheme >= 0) value = value[(scheme + 3)..];
        var slash = value.IndexOf('/');
        if (slash >= 0) value = value[..slash];
        return value.Trim();
    }

    private IEnumerable<string> ReadLines(string file)
    {
        var path = Path.Combine(ListsDir, file);
        return File.Exists(path)
            ? File.ReadLines(path).Where(l => !string.IsNullOrWhiteSpace(l))
            : Enumerable.Empty<string>();
    }

    private static void WriteAtomic(string path, string content)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, content);
        File.Move(temp, path, overwrite: true);
    }

    private void EnsureUserFile(string name, string fallback)
    {
        var path = Path.Combine(ListsDir, name);
        if (!File.Exists(path)) File.WriteAllText(path, fallback);
    }
}
