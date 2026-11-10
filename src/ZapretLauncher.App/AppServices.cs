using System.IO;
using System.Net.Http;
using ZapretLauncher.Core.Coverage;
using ZapretLauncher.Core.Diagnostics;
using ZapretLauncher.Core.Engine;
using ZapretLauncher.Core.Probing;
using ZapretLauncher.Core.Runtime;
using ZapretLauncher.Core.Settings;
using ZapretLauncher.Core.Strategies;
using ZapretLauncher.Core.Testing;
using ZapretLauncher.Core.Updates;

namespace ZapretLauncher.App;

public sealed class AppServices : IDisposable
{
    private bool _disposed;

    public AppServices()
    {
        DataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZapretLauncher");
        Directory.CreateDirectory(DataRoot);

        Log = new LogService(DataRoot);
        SettingsStorage = new SettingsStore(DataRoot);
        Settings = SettingsStorage.Load();
        Engine = new EngineStore(DataRoot);
        EngineDir = Engine.EnsureEngine();
        Catalog = StrategyCatalog.LoadBuiltin();
        Catalog.ApplyUserOverrides(Path.Combine(DataRoot, "strategies-user.json"));

        ProbeHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        UpdateHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        Coverage = new CoverageService(DataRoot, UpdateHttp, Log);
        Coverage.EnsureDefaults(EngineDir);
        Coverage.Apply(Settings);

        Runner = new BypassRunner(Log);
        Builder = new CommandBuilder();
        Probe = new SiteProbe(ProbeHttp);
        Diagnostics = new DiagnosticsService(new ProcessRunner(), EngineDir);
        Tester = new StrategyTester(Runner, Probe, Builder, new SystemDelay());
        Updates = new UpdateService(UpdateHttp, Engine, Log);

        if (string.IsNullOrWhiteSpace(Settings.SelectedStrategyId))
        {
            Settings.SelectedStrategyId =
                Catalog.All.FirstOrDefault(s => s.Id == "alt11")?.Id
                ?? Catalog.All.First().Id;
        }
    }

    public string DataRoot { get; }
    public LogService Log { get; }
    public SettingsStore SettingsStorage { get; }
    public AppSettings Settings { get; private set; }
    public EngineStore Engine { get; }
    public string EngineDir { get; }
    public StrategyCatalog Catalog { get; }
    public HttpClient ProbeHttp { get; }
    public HttpClient UpdateHttp { get; }
    public CoverageService Coverage { get; }
    public BypassRunner Runner { get; }
    public CommandBuilder Builder { get; }
    public SiteProbe Probe { get; }
    public DiagnosticsService Diagnostics { get; }
    public StrategyTester Tester { get; }
    public UpdateService Updates { get; }

    public BuildContext BuildContext
    {
        get
        {
            var (tcp, udp) = CoverageService.GameValues(Settings.GameFilter);
            return new BuildContext(EngineDir, Coverage.ListsDir, tcp, udp);
        }
    }

    public void SaveSettings() => SettingsStorage.Save(Settings);

    public void ResetSettings()
    {
        Settings = new AppSettings();
        SaveSettings();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Runner.Dispose();
        ProbeHttp.Dispose();
        UpdateHttp.Dispose();
    }
}
