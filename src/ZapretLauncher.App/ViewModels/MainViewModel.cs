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

    [ObservableProperty] private object? selectedContent;

    public Task InitializeAsync() => Status.InitializeAsync();
}
