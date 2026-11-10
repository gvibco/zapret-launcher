using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZapretLauncher.Core.Models;
using ZapretLauncher.Core.Networking;
using ZapretLauncher.Core.Probing;
using ZapretLauncher.Core.Settings;
using ZapretLauncher.Core.Testing;

namespace ZapretLauncher.App.ViewModels;

public sealed partial class StrategyRow : ObservableObject
{
    public StrategyRow(Strategy strategy)
    {
        Strategy = strategy;
        Modes = string.Join(", ", strategy.Profiles
            .SelectMany(p => p.Desync.Modes).Distinct());
    }

    public Strategy Strategy { get; }
    public string Name => Strategy.Name;
    public string Modes { get; }

    [ObservableProperty] private string score = "—";
    [ObservableProperty] private string testedAt = "";
}

public partial class StrategiesViewModel : ObservableObject
{
    private readonly AppServices _services;
    private CancellationTokenSource? _cts;

    public StrategiesViewModel(AppServices services)
    {
        _services = services;
        foreach (var strategy in services.Catalog.All)
            Strategies.Add(new StrategyRow(strategy));
        Selected = Strategies.FirstOrDefault(r =>
            r.Strategy.Id == services.Settings.SelectedStrategyId) ?? Strategies.FirstOrDefault();
    }

    public ObservableCollection<StrategyRow> Strategies { get; } = new();

    [ObservableProperty] private StrategyRow? selected;
    [ObservableProperty] private string argsPreview = "";
    [ObservableProperty] private string progressText = "";
    [ObservableProperty] private bool isTesting;

    partial void OnSelectedChanged(StrategyRow? value)
    {
        if (value is null)
        {
            ArgsPreview = "";
            return;
        }
        var args = _services.Builder.Build(value.Strategy, _services.BuildContext);
        ArgsPreview = string.Join(Environment.NewLine, args);
    }

    [RelayCommand]
    private async Task RunSelectedAsync()
    {
        if (Selected is null) return;
        await StartAsync(Selected.Strategy);
        ProgressText = $"Запущено: {Selected.Name}";
    }

    [RelayCommand]
    private async Task TestAllAsync()
    {
        if (IsTesting) return;
        _cts = new CancellationTokenSource();
        IsTesting = true;
        ProgressText = "Подготовка...";
        try
        {
            var progress = new Progress<TestProgress>(p =>
            {
                ProgressText = $"{p.Index}/{p.Total}: {p.StrategyName}";
                _services.Log.Write($"Test {p.Index}/{p.Total}: {p.StrategyName}");
            });
            var extraTargets = _services.Settings.Targets
                .Where(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .Select(url => new ProbeTarget(url, url, false));
            var targets = SiteProbe.DefaultTargets.Concat(extraTargets).ToList();

            _services.Log.Write("Test started");
            var results = await _services.Tester.TestAllAsync(
                _services.Catalog.All, _services.BuildContext, targets, progress, _cts.Token);
            _services.Log.Write("Test finished");

            foreach (var result in results)
            {
                var row = Strategies.FirstOrDefault(r => r.Strategy.Id == result.StrategyId);
                if (row is null) continue;
                row.Score = $"{result.Score.CriticalPassed}/{result.Score.TotalPassed}";
                row.TestedAt = result.DryRunPassed ? "только что" : "невалидна";
            }

            var best = StrategyTester.ChooseBest(results);
            if (best is null)
            {
                ProgressText = "Рабочая стратегия не найдена";
                return;
            }

            _services.Settings.SelectedStrategyId = best.StrategyId;
            _services.Settings.LastGoodByNetwork[new NetworkKeyProvider().GetKey()] = new NetworkResult
            {
                StrategyId = best.StrategyId,
                Score = best.Score,
                TestedAt = DateTimeOffset.Now,
                EngineVersion = _services.Engine.CurrentVersion,
                IpsetMode = _services.Settings.IpsetMode,
                GameFilterMode = _services.Settings.GameFilter.Mode
            };
            _services.SaveSettings();

            Selected = Strategies.First(r => r.Strategy.Id == best.StrategyId);
            await StartAsync(Selected.Strategy);
            ProgressText = $"Лучшая: {Selected.Name}";
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Тест отменён";
        }
        catch (Exception ex)
        {
            ProgressText = "Ошибка теста: " + ex.Message;
            _services.Log.Write(ex.ToString());
        }
        finally
        {
            IsTesting = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void CancelTest() => _cts?.Cancel();

    [RelayCommand]
    private void CopyArgs() => Clipboard.SetText(ArgsPreview);

    private async Task StartAsync(Strategy strategy)
    {
        if (_services.Runner.IsRunning) await _services.Runner.StopAsync();
        var args = _services.Builder.Build(strategy, _services.BuildContext);
        await _services.Runner.StartAsync(
            Path.Combine(_services.EngineDir, "winws.exe"), args);
        _services.Settings.SelectedStrategyId = strategy.Id;
        _services.SaveSettings();
    }
}
