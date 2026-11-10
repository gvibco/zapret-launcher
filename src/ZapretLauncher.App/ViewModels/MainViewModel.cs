using CommunityToolkit.Mvvm.ComponentModel;

namespace ZapretLauncher.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel(AppServices services)
    {
        Services = services;
        Status = new StatusViewModel(services);
        Strategies = new StrategiesViewModel(services);
        Coverage = new CoverageViewModel(services);
        Diagnostics = new DiagnosticsViewModel(services);
        Settings = new SettingsViewModel(services);
        Logs = new LogsViewModel(services);
        SelectedContent = Status;
        Status.TestAllRequested += () =>
        {
            SelectedContent = Strategies;
            Strategies.TestAllCommand.Execute(null);
        };
    }

    public AppServices Services { get; }
    public StatusViewModel Status { get; }
    public StrategiesViewModel Strategies { get; }
    public CoverageViewModel Coverage { get; }
    public DiagnosticsViewModel Diagnostics { get; }
    public SettingsViewModel Settings { get; }
    public LogsViewModel Logs { get; }

    [ObservableProperty] private object? selectedContent;

    public Task InitializeAsync() => Status.InitializeAsync();
}
