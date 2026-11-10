using System.Threading;
using System.Windows;
using ZapretLauncher.App.ViewModels;

namespace ZapretLauncher.App;

public partial class App : Application
{
    private Mutex? _mutex;
    private AppServices? _services;
    private MainWindow? _window;
    private TrayService? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "ZapretLauncher.SingleInstance", out var created);
        if (!created)
        {
            MessageBox.Show("Zapret Launcher уже запущен.", "Zapret Launcher");
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            _services?.Log.Write(args.Exception.ToString());
            MessageBox.Show("Ошибка: " + args.Exception.Message, "Zapret Launcher");
            args.Handled = true;
        };

        _services = new AppServices();
        var viewModel = new MainViewModel(_services);
        _window = new MainWindow { DataContext = viewModel };

        _tray = new TrayService(
            _services.Catalog.All,
            select: id => _ = viewModel.Status.SwitchToAsync(id),
            toggle: () => viewModel.Status.ToggleCommand.Execute(null),
            check: () => viewModel.Status.CheckNowCommand.Execute(null),
            open: OpenWindow,
            exit: ExitApplication);

        if (!_services.Settings.StartMinimized) OpenWindow();
        _ = viewModel.InitializeAsync();
        base.OnStartup(e);
    }

    private void OpenWindow()
    {
        if (_window is null) return;
        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    private void ExitApplication()
    {
        _window?.AllowClose();
        _tray?.Dispose();
        _services?.Dispose();
        Shutdown();
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        _window?.AllowClose();
        base.OnSessionEnding(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _services?.Dispose();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
