using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ZapretLauncher.App.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private const int MaxLines = 1000;

    private readonly AppServices _services;
    private readonly List<string> _all = new();
    private readonly ConcurrentQueue<string> _pending = new();
    private readonly DispatcherTimer _flushTimer;
    private int _timerScheduled;

    public LogsViewModel(AppServices services)
    {
        _services = services;
        _flushTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _flushTimer.Tick += (_, _) => Flush();
        _services.Log.LineWritten += OnLineWritten;
    }

    public ObservableCollection<string> Lines { get; } = new();

    public bool HasLines => Lines.Count > 0;

    [ObservableProperty] private string filter = "";

    partial void OnFilterChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void Clear()
    {
        _pending.Clear();
        _all.Clear();
        Lines.Clear();
        OnPropertyChanged(nameof(HasLines));
    }

    [RelayCommand]
    private void CopyAll() =>
        Clipboard.SetText(string.Join(Environment.NewLine, Lines));

    [RelayCommand]
    private void OpenFolder()
    {
        var folder = Path.GetDirectoryName(_services.Log.AppLogPath) ?? _services.DataRoot;
        Process.Start(new ProcessStartInfo("explorer.exe", folder) { UseShellExecute = true });
    }

    private void OnLineWritten(string line)
    {
        _pending.Enqueue(line);
        if (Interlocked.Exchange(ref _timerScheduled, 1) == 0)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null) return;
            dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_flushTimer.IsEnabled) _flushTimer.Start();
            }));
        }
    }

    /// <summary>
    /// Applies buffered winws output at most four times per second. Rebuilding the
    /// visible list on every single log line caused noticeable UI churn.
    /// </summary>
    private void Flush()
    {
        if (_pending.IsEmpty)
        {
            StopTimer();
            return;
        }

        var filter = Filter.Trim();
        var canAppend = filter.Length == 0 && Lines.Count == _all.Count;
        var dropped = false;

        while (_pending.TryDequeue(out var line))
        {
            _all.Add(line);
            if (_all.Count > MaxLines)
            {
                _all.RemoveAt(0);
                dropped = true;
            }

            if (canAppend && !dropped) Lines.Add(line);
        }

        if (!canAppend || dropped) ApplyFilter();
        OnPropertyChanged(nameof(HasLines));
        if (_pending.IsEmpty) StopTimer();
    }

    private void StopTimer()
    {
        _flushTimer.Stop();
        Interlocked.Exchange(ref _timerScheduled, 0);
        if (!_pending.IsEmpty && Interlocked.Exchange(ref _timerScheduled, 1) == 0)
            _flushTimer.Start();
    }

    private void ApplyFilter()
    {
        Lines.Clear();
        var filter = Filter.Trim();
        foreach (var line in _all.Where(l =>
                     filter.Length == 0 ||
                     l.Contains(filter, StringComparison.OrdinalIgnoreCase)))
        {
            Lines.Add(line);
        }
        OnPropertyChanged(nameof(HasLines));
    }
}
