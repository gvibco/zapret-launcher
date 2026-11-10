namespace ZapretLauncher.Core.Probing;

public enum FailureKind
{
    None,
    Dns,
    Tls,
    Reset,
    Timeout,
    Http,
    Other
}

public sealed record ProbeTarget(string Name, string Url, bool Critical);

public sealed record ProbeResult(
    string Name,
    string Url,
    bool Critical,
    bool Success,
    int? StatusCode,
    long ElapsedMs,
    FailureKind Failure,
    string? Error);

public interface IProbe
{
    Task<ProbeResult> ProbeAsync(ProbeTarget target, CancellationToken ct = default);
}
