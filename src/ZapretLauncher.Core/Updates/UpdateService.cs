using System.Text.Json;
using ZapretLauncher.Core.Engine;
using ZapretLauncher.Core.Runtime;

namespace ZapretLauncher.Core.Updates;

public sealed record UpdateInfo(string Version, string DownloadUrl);

public sealed class UpdateService
{
    private const string LatestUrl =
        "https://api.github.com/repos/Flowseal/zapret-discord-youtube/releases/latest";

    private readonly HttpClient _http;
    private readonly EngineStore _engine;
    private readonly LogService _log;

    public UpdateService(HttpClient http, EngineStore engine, LogService log)
    {
        _http = http;
        _engine = engine;
        _log = log;
    }

    public async Task<UpdateInfo?> GetLatestAsync(CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestUrl);
        request.Headers.UserAgent.ParseAdd("ZapretLauncher/1.0");
        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = document.RootElement;
        var version = root.GetProperty("tag_name").GetString()?.TrimStart('v', 'V') ?? "";
        if (version.Length == 0) return null;

        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) continue;
            var url = asset.GetProperty("browser_download_url").GetString();
            if (!string.IsNullOrWhiteSpace(url)) return new UpdateInfo(version, url);
        }
        return null;
    }

    public async Task<string> DownloadAndInstallAsync(UpdateInfo info, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(
            info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        var path = _engine.InstallUpdate(buffer, info.Version);
        _log.Write($"Engine updated to {info.Version}");
        return path;
    }
}
