using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ZapretLauncher.App.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private readonly AppServices _services;
    private readonly List<string> _all = new();

    public LogsViewModel(AppServices services)
    {
        _services = services;
        _services.Log.LineWritten += line =>
            Application.Current.Dispatcher.Invoke(() => AddLine(line));
    }

    public ObservableCollection<string> Lines { get; } = new();

    [ObservableProperty] private string filter = "";

    partial void OnFilterChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void Clear()
    {
        _all.Clear();
        Lines.Clear();
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

    private void AddLine(string line)
    {
        _all.Add(line);
        while (_all.Count > 1000) _all.RemoveAt(0);
        ApplyFilter();
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
    }
}
