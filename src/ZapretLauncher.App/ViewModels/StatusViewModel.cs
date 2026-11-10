using System.Collections.ObjectModel;
using System.IO;
using System.Security.Principal;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZapretLauncher.Core.Diagnostics;
using ZapretLauncher.Core.Networking;
using ZapretLauncher.Core.Probing;
using ZapretLauncher.Core.Runtime;

namespace ZapretLauncher.App.ViewModels;

public partial class StatusViewModel : ObservableObject
{
    private readonly AppServices _services;

    public StatusViewModel(AppServices services)
    {
        _services = services;
        _services.Runner.StateChanged += (_, state) =>
            Application.Current.Dispatcher.BeginInvoke(() => IsRunning = state == BypassState.Running);
    }

    public ObservableCollection<string> Warnings { get; } = new();

    public bool HasWarnings => Warnings.Count > 0;

    [ObservableProperty] private bool isRunning;
    [ObservableProperty] private string toggleLabel = "Включить обход";
    [ObservableProperty] private string currentStrategyName = "";
    [ObservableProperty] private string engineVersion = "";
    [ObservableProperty] private string networkName = "";
    [ObservableProperty] private string adminText = "";
    [ObservableProperty] private string statusMessage = "";
    [ObservableProperty] private string checkSummary = "";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool showFirstRunBanner;
    [ObservableProperty] private string youTubeStatus = "—";
    [ObservableProperty] private string discordStatus = "—";
    [ObservableProperty] private string googleStatus = "—";

    partial void OnIsRunningChanged(bool value) =>
        ToggleLabel = value ? "Выключить обход" : "Включить обход";

    public async Task InitializeAsync()
    {
        var networkKey = new NetworkKeyProvider().GetKey();
        if (_services.Settings.LastGoodByNetwork.TryGetValue(networkKey, out var known)
            && _services.Catalog.All.Any(s => s.Id == known.StrategyId))
        {
            _services.Settings.SelectedStrategyId = known.StrategyId;
        }

        var selected = _services.Catalog.All
            .FirstOrDefault(s => s.Id == _services.Settings.SelectedStrategyId)
            ?? _services.Catalog.All[0];
        _services.Settings.SelectedStrategyId = selected.Id;
        CurrentStrategyName = selected.Name;
        EngineVersion = _services.Engine.CurrentVersion;
        var key = networkKey.Split('|');
        NetworkName = key.Length >= 3 && key[2].Length > 0 ? key[2] : "Проводная сеть";
        AdminText = IsAdmin() ? "Администратор" : "Нет прав администратора";
        ShowFirstRunBanner = !_services.Settings.LastGoodByNetwork.ContainsKey(networkKey);
        await RefreshDiagnosticsAsync();

        if (_services.Settings.LastGoodByNetwork.ContainsKey(networkKey))
        {
            try
            {
                await StartSelectedAsync();
                StatusMessage = "Обход запущен автоматически.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Автозапуск обхода не удался: " + ex.Message;
                _services.Log.Write(ex.ToString());
            }
        }
    }

    [RelayCommand]
    private async Task ToggleAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            if (IsRunning || _services.Runner.IsRunning)
            {
                await _services.Runner.StopAsync();
                IsRunning = _services.Runner.IsRunning;
                StatusMessage = IsRunning
                    ? "Не удалось остановить обход."
                    : "Обход выключен.";
                return;
            }

            await StartSelectedAsync();
            StatusMessage = $"Обход включён: {CurrentStrategyName}.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Ошибка запуска: " + ex.Message;
            _services.Log.Write(ex.ToString());
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CheckNowAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var results = await Task.WhenAll(
                SiteProbe.DefaultTargets.Select(t => _services.Probe.ProbeAsync(t)));
            var passed = results.Count(r => r.Success);
            var failed = results.Where(r => !r.Success)
                .Select(r => $"{r.Name} ({r.Failure})");
            CheckSummary = $"Прошло {passed} из {results.Length}" +
                           (failed.Any() ? ". Не прошли: " + string.Join(", ", failed) : "");
            YouTubeStatus = Summarize(results, "YouTube");
            DiscordStatus = Summarize(results, "Discord");
            GoogleStatus = Summarize(results, "Google");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshDiagnosticsAsync()
    {
        var report = await _services.Diagnostics.RunAsync();
        Warnings.Clear();
        foreach (var check in report.Checks.Where(c => c.Status != DiagnosticStatus.Pass))
            Warnings.Add($"{check.Name}: {check.Message}");

        if (_services.Settings.IpsetMode == "none")
            Warnings.Add("IPSet Filter выключен: игры и трафик без SNI могут не обходиться.");
        if (_services.Settings.GameFilter.Mode == "disabled")
            Warnings.Add("Game Filter выключен: UDP-игры могут не работать.");

        OnPropertyChanged(nameof(HasWarnings));
    }

    [RelayCommand]
    private void DismissFirstRun() => ShowFirstRunBanner = false;

    public event Action? TestAllRequested;

    [RelayCommand]
    private void RequestTestAll() => TestAllRequested?.Invoke();

    public async Task SwitchToAsync(string strategyId)
    {
        try
        {
            _services.Settings.SelectedStrategyId = strategyId;
            _services.SaveSettings();
            if (_services.Runner.IsRunning) await _services.Runner.StopAsync();
            await StartSelectedAsync();
            StatusMessage = $"Переключено на {CurrentStrategyName}.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Ошибка переключения: " + ex.Message;
            _services.Log.Write(ex.ToString());
        }
    }

    private static string Summarize(IReadOnlyList<ProbeResult> results, string prefix)
    {
        var group = results
            .Where(r => r.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return group.Count == 0 ? "—" : $"{group.Count(r => r.Success)}/{group.Count}";
    }

    private async Task StartSelectedAsync()
    {
        var strategy = _services.Catalog.Get(_services.Settings.SelectedStrategyId!);
        var args = _services.Builder.Build(strategy, _services.BuildContext);
        await _services.Runner.StartAsync(
            Path.Combine(_services.EngineDir, "winws.exe"), args);
        IsRunning = _services.Runner.IsRunning;
        CurrentStrategyName = strategy.Name;
    }

    private static bool IsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
