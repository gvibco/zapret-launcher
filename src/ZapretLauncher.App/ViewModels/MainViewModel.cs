using CommunityToolkit.Mvvm.ComponentModel;

namespace ZapretLauncher.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel(AppServices services)
    {
        Services = services;
        Status = new StatusViewModel(services);
        SelectedContent = Status;
    }

    public AppServices Services { get; }
    public StatusViewModel Status { get; }

    [ObservableProperty] private object? selectedContent;

    public Task InitializeAsync() => Status.InitializeAsync();
}
