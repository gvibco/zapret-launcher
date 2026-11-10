namespace ZapretLauncher.Core.Diagnostics;

public enum DiagnosticStatus
{
    Pass,
    Warn,
    Fail
}

public sealed record DiagnosticCheck(string Name, DiagnosticStatus Status, string Message);

public sealed record DiagnosticReport(IReadOnlyList<DiagnosticCheck> Checks)
{
    public bool HasFailures => Checks.Any(c => c.Status == DiagnosticStatus.Fail);
}
