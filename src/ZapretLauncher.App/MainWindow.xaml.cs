using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ZapretLauncher.App.ViewModels;

namespace ZapretLauncher.App;

public partial class MainWindow : Window
{
    private bool _allowClose;

    public MainWindow() => InitializeComponent();

    public void AllowClose() => _allowClose = true;

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose)
        {
            if (DataContext is MainViewModel { Services.Settings.CloseToTray: true })
            {
                e.Cancel = true;
                Hide();
                return;
            }

            _allowClose = true;
            Application.Current.Shutdown();
        }
        base.OnClosing(e);
    }

    private void Nav_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel) return;
        if (Nav.SelectedItem is not ListBoxItem item) return;
        viewModel.SelectedContent = item.Tag switch
        {
            "status" => viewModel.Status,
            "strategies" => viewModel.Strategies,
            "coverage" => viewModel.Coverage,
            "diagnostics" => viewModel.Diagnostics,
            "settings" => viewModel.Settings,
            "logs" => viewModel.Logs,
            _ => viewModel.Status
        };
    }
}
