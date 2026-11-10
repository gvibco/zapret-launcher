using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;

namespace ZapretLauncher.Core.Diagnostics;

public interface IProcessRunner
{
    Task<(int ExitCode, string Output)> RunAsync(
        string fileName, string arguments, CancellationToken ct = default);
}

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<(int ExitCode, string Output)> RunAsync(
        string fileName, string arguments, CancellationToken ct = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return (process.ExitCode, await outputTask + await errorTask);
    }
}

public sealed class DiagnosticsService
{
    public static readonly string[] ConflictingServices =
    {
        "GoodbyeDPI", "discordfix_zapret", "winws1", "winws2"
    };

    private readonly IProcessRunner _runner;
    private readonly string _engineDir;

    public DiagnosticsService(IProcessRunner runner, string engineDir)
    {
        _runner = runner;
        _engineDir = engineDir;
    }

    public async Task<DiagnosticReport> RunAsync(CancellationToken ct = default)
    {
        var checks = new List<DiagnosticCheck>();

        checks.Add(IsAdmin()
            ? new DiagnosticCheck("Права администратора", DiagnosticStatus.Pass, "Запущено от администратора")
            : new DiagnosticCheck("Права администратора", DiagnosticStatus.Fail,
                "Нужны права администратора: WinDivert не загрузится"));

        var engineOk = new[] { "winws.exe", "WinDivert.dll", "WinDivert64.sys", "cygwin1.dll" }
            .All(f => File.Exists(Path.Combine(_engineDir, f)));
        checks.Add(engineOk
            ? new DiagnosticCheck("Файлы движка", DiagnosticStatus.Pass, "Все файлы на месте")
            : new DiagnosticCheck("Файлы движка", DiagnosticStatus.Fail,
                "Не хватает файлов winws/WinDivert/cygwin"));

        var bfe = await _runner.RunAsync("sc", "query BFE", ct);
        checks.Add(bfe.Output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase)
            ? new DiagnosticCheck("Base Filtering Engine", DiagnosticStatus.Pass, "Служба запущена")
            : new DiagnosticCheck("Base Filtering Engine", DiagnosticStatus.Fail,
                "Служба BFE не запущена, обход работать не будет"));

        var windivert = await _runner.RunAsync("sc", "query WinDivert", ct);
        checks.Add(windivert.Output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase)
            ? new DiagnosticCheck("WinDivert", DiagnosticStatus.Pass, "Драйвер запущен")
            : new DiagnosticCheck("WinDivert", DiagnosticStatus.Warn,
                "Драйвер не запущен (появится после старта winws)"));

        var timestamps = await _runner.RunAsync("netsh", "interface tcp show global", ct);
        checks.Add(TimestampsEnabled(timestamps.Output)
            ? new DiagnosticCheck("TCP timestamps", DiagnosticStatus.Pass, "Включены")
            : new DiagnosticCheck("TCP timestamps", DiagnosticStatus.Warn,
                "Выключены: fooling=ts не сработает"));

        checks.Add(CheckProxy());
        checks.Add(await CheckConflictsAsync(ct));
        checks.Add(CheckHosts());

        return new DiagnosticReport(checks);
    }

    public static bool TimestampsEnabled(string netshOutput) =>
        netshOutput.Split('\n')
            .Any(line => line.Contains("Timestamps", StringComparison.OrdinalIgnoreCase)
                         && line.Contains("enabled", StringComparison.OrdinalIgnoreCase));

    public static bool HasYoutubeHosts(string hosts) =>
        hosts.Split('\n')
            .Any(line => line.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
                         || line.Contains("youtu.be", StringComparison.OrdinalIgnoreCase));

    private static DiagnosticCheck CheckProxy()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Internet Settings");
        var enabled = key?.GetValue("ProxyEnable") is int value && value == 1;
        var server = key?.GetValue("ProxyServer") as string;
        return enabled
            ? new DiagnosticCheck("Системный прокси", DiagnosticStatus.Warn,
                $"Прокси включён: {server}. Убедитесь, что он рабочий")
            : new DiagnosticCheck("Системный прокси", DiagnosticStatus.Pass, "Выключен");
    }

    private async Task<DiagnosticCheck> CheckConflictsAsync(CancellationToken ct)
    {
        var found = new List<string>();
        foreach (var name in ConflictingServices)
        {
            var result = await _runner.RunAsync("sc", $"query \"{name}\"", ct);
            if (result.Output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase))
                found.Add(name);
        }

        var adguard = await _runner.RunAsync(
            "tasklist", "/FI \"IMAGENAME eq AdguardSvc.exe\"", ct);
        if (adguard.Output.Contains("AdguardSvc.exe", StringComparison.OrdinalIgnoreCase))
            found.Add("AdguardSvc.exe");

        return found.Count == 0
            ? new DiagnosticCheck("Конфликты", DiagnosticStatus.Pass, "Не найдены")
            : new DiagnosticCheck("Конфликты", DiagnosticStatus.Warn,
                "Найдены: " + string.Join(", ", found));
    }

    private static DiagnosticCheck CheckHosts()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
        if (!File.Exists(path))
            return new DiagnosticCheck("hosts", DiagnosticStatus.Warn, "Файл hosts не найден");

        return HasYoutubeHosts(File.ReadAllText(path))
            ? new DiagnosticCheck("hosts", DiagnosticStatus.Warn,
                "В hosts есть youtube.com/youtu.be: это может мешать YouTube")
            : new DiagnosticCheck("hosts", DiagnosticStatus.Pass, "Записей YouTube нет");
    }

    private static bool IsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
