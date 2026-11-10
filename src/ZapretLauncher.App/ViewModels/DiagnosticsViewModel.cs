using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZapretLauncher.Core.Probing;

namespace ZapretLauncher.App.ViewModels;

public sealed record DiagnosticRow(string Name, string Status, string Message);
public sealed record ProbeRow(string Name, string Result, string Latency, string Failure);

public partial class DiagnosticsViewModel : ObservableObject
{
    private readonly AppServices _services;

    public DiagnosticsViewModel(AppServices services) => _services = services;

    public ObservableCollection<DiagnosticRow> Checks { get; } = new();
    public ObservableCollection<ProbeRow> Probes { get; } = new();
    [ObservableProperty] private string statusText = "Нажмите «Проверить», чтобы запустить диагностику.";
    [ObservableProperty] private bool isBusy;

    [RelayCommand]
    private async Task RunAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var report = await _services.Diagnostics.RunAsync();
            Checks.Clear();
            foreach (var check in report.Checks)
                Checks.Add(new DiagnosticRow(check.Name, check.Status.ToString(), check.Message));

            var probes = await Task.WhenAll(
                SiteProbe.DefaultTargets.Select(t => _services.Probe.ProbeAsync(t)));
            Probes.Clear();
            foreach (var probe in probes)
            {
                Probes.Add(new ProbeRow(
                    probe.Name,
                    probe.Success ? "OK" : "FAIL",
                    $"{probe.ElapsedMs} ms",
                    probe.Success ? "—" : probe.Failure.ToString()));
            }

            StatusText = report.HasFailures
                ? "Есть критические проблемы."
                : "Критических проблем не найдено.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CopyReport()
    {
        var text = string.Join(Environment.NewLine,
            Checks.Select(c => $"[{c.Status}] {c.Name}: {c.Message}"));
        Clipboard.SetText(text);
        StatusText = "Отчёт скопирован.";
    }
}
