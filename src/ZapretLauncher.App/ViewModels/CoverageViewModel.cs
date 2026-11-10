using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZapretLauncher.Core.Settings;

namespace ZapretLauncher.App.ViewModels;

public sealed partial class DomainRow : ObservableObject
{
    public DomainRow(string domain, bool exact)
    {
        Domain = domain;
        Exact = exact;
    }

    public string Domain { get; }
    [ObservableProperty] private bool exact;
}

public partial class CoverageViewModel : ObservableObject
{
    private readonly AppServices _services;

    public CoverageViewModel(AppServices services)
    {
        _services = services;
        var settings = services.Settings;
        foreach (var domain in settings.CustomDomains)
            Domains.Add(new DomainRow(domain.Domain, domain.Exact));
        foreach (var exclusion in settings.CustomExclusions) Exclusions.Add(exclusion);
        foreach (var ip in settings.CustomIps) Ips.Add(ip);
        SelectedIpsetMode = settings.IpsetMode;
        SelectedGameMode = settings.GameFilter.Mode;
        TcpRange = settings.GameFilter.TcpRange;
        UdpRange = settings.GameFilter.UdpRange;
        RefreshListsStatus();
    }

    public ObservableCollection<DomainRow> Domains { get; } = new();
    public ObservableCollection<string> Exclusions { get; } = new();
    public ObservableCollection<string> Ips { get; } = new();
    public IReadOnlyList<string> IpsetModes { get; } = new[] { "none", "loaded", "any" };
    public IReadOnlyList<string> GameModes { get; } = new[] { "disabled", "tcp", "udp", "all" };

    [ObservableProperty] private string selectedIpsetMode = "none";
    [ObservableProperty] private string selectedGameMode = "disabled";
    [ObservableProperty] private string tcpRange = "1024-65535";
    [ObservableProperty] private string udpRange = "1024-65535";
    [ObservableProperty] private string newDomain = "";
    [ObservableProperty] private bool newDomainExact;
    [ObservableProperty] private string newExclusion = "";
    [ObservableProperty] private string newIp = "";
    [ObservableProperty] private string listsStatus = "";
    [ObservableProperty] private string statusMessage = "";
    [ObservableProperty] private bool isBusy;

    [RelayCommand]
    private void AddDomain()
    {
        var domain = NewDomain.Trim();
        if (domain.Length == 0) return;
        Domains.Add(new DomainRow(domain, NewDomainExact));
        NewDomain = "";
        NewDomainExact = false;
    }

    [RelayCommand]
    private void RemoveDomain(DomainRow row) => Domains.Remove(row);

    [RelayCommand]
    private void AddExclusion()
    {
        var value = NewExclusion.Trim();
        if (value.Length == 0) return;
        Exclusions.Add(value);
        NewExclusion = "";
    }

    [RelayCommand]
    private void RemoveExclusion(string value) => Exclusions.Remove(value);

    [RelayCommand]
    private void AddIp()
    {
        var value = NewIp.Trim();
        if (value.Length == 0) return;
        Ips.Add(value);
        NewIp = "";
    }

    [RelayCommand]
    private void RemoveIp(string value) => Ips.Remove(value);

    [RelayCommand]
    private async Task UpdateListsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var ok = await _services.Coverage.UpdateListsAsync(_services.Settings.ListBaseUrl);
            StatusMessage = ok ? "Списки обновлены." : "Не все списки обновились, старые сохранены.";
            RefreshListsStatus();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var settings = _services.Settings;
            settings.IpsetMode = SelectedIpsetMode;
            settings.GameFilter.Mode = SelectedGameMode;
            settings.GameFilter.TcpRange = TcpRange;
            settings.GameFilter.UdpRange = UdpRange;
            settings.CustomDomains = Domains
                .Select(d => new CustomDomain { Domain = d.Domain, Exact = d.Exact })
                .ToList();
            settings.CustomExclusions = Exclusions.ToList();
            settings.CustomIps = Ips.ToList();
            _services.SaveSettings();
            _services.Coverage.Apply(settings);

            if (_services.Runner.IsRunning)
            {
                await _services.Runner.StopAsync();
                var strategy = _services.Catalog.Get(settings.SelectedStrategyId!);
                var args = _services.Builder.Build(strategy, _services.BuildContext);
                await _services.Runner.StartAsync(
                    Path.Combine(_services.EngineDir, "winws.exe"), args);
            }

            StatusMessage = "Покрытие применено.";
            RefreshListsStatus();
        }
        catch (Exception ex)
        {
            StatusMessage = "Ошибка: " + ex.Message;
            _services.Log.Write(ex.ToString());
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshListsStatus()
    {
        var file = Path.Combine(_services.Coverage.ListsDir, "list-general.txt");
        ListsStatus = File.Exists(file)
            ? $"list-general: {File.ReadAllLines(file).Length} строк, изменён {File.GetLastWriteTime(file):g}"
            : "Списки ещё не загружены";
    }
}
