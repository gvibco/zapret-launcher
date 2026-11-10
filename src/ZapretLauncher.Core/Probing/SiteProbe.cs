using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Authentication;

namespace ZapretLauncher.Core.Probing;

public sealed class SiteProbe : IProbe
{
    public static readonly IReadOnlyList<ProbeTarget> DefaultTargets = new[]
    {
        new ProbeTarget("Discord", "https://discord.com", true),
        new ProbeTarget("Discord Gateway", "https://gateway.discord.gg", true),
        new ProbeTarget("Discord CDN", "https://cdn.discordapp.com", true),
        new ProbeTarget("Discord Updates", "https://updates.discord.com", true),
        new ProbeTarget("YouTube", "https://www.youtube.com", true),
        new ProbeTarget("YouTube Short", "https://youtu.be", true),
        new ProbeTarget("YouTube Images", "https://i.ytimg.com", true),
        new ProbeTarget("YouTube Redirector", "https://redirector.googlevideo.com", true),
        new ProbeTarget("Google", "https://www.google.com", false),
        new ProbeTarget("Google Static", "https://www.gstatic.com", false),
        new ProbeTarget("Cloudflare", "https://www.cloudflare.com", false),
        new ProbeTarget("Cloudflare CDN", "https://cdnjs.cloudflare.com", false)
    };

    private readonly HttpClient _http;

    public SiteProbe(HttpClient http) => _http = http;

    public async Task<ProbeResult> ProbeAsync(ProbeTarget target, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, target.Url);
            using var response = await _http.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, ct);
            stopwatch.Stop();
            var status = (int)response.StatusCode;
            var ok = status < 500;
            return new ProbeResult(
                target.Name, target.Url, target.Critical, ok,
                status, stopwatch.ElapsedMilliseconds,
                ok ? FailureKind.None : FailureKind.Http,
                ok ? null : $"HTTP {status}");
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return new ProbeResult(
                target.Name, target.Url, target.Critical, false, null,
                stopwatch.ElapsedMilliseconds, FailureKind.Timeout, "timeout");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ProbeResult(
                target.Name, target.Url, target.Critical, false, null,
                stopwatch.ElapsedMilliseconds, Classify(ex), ex.Message);
        }
    }

    public static FailureKind Classify(Exception exception)
    {
        if (exception is OperationCanceledException) return FailureKind.Timeout;
        if (exception is not HttpRequestException http) return FailureKind.Other;

        return http.InnerException switch
        {
            AuthenticationException => FailureKind.Tls,
            SocketException socket => socket.SocketErrorCode switch
            {
                SocketError.HostNotFound or SocketError.NoData => FailureKind.Dns,
                SocketError.ConnectionReset or SocketError.ConnectionAborted => FailureKind.Reset,
                SocketError.TimedOut => FailureKind.Timeout,
                _ => FailureKind.Other
            },
            _ => FailureKind.Other
        };
    }
}
