using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using Hardcodet.Wpf.TaskbarNotification;
using ZapretLauncher.Core.Models;

namespace ZapretLauncher.App;

public sealed class TrayService : IDisposable
{
    private readonly TaskbarIcon _icon;

    public TrayService(
        IReadOnlyList<Strategy> strategies,
        Action<string> select,
        Action toggle,
        Action check,
        Action open,
        Action exit)
    {
        _icon = new TaskbarIcon
        {
            Icon = SystemIcons.Application,
            ToolTipText = "Zapret Launcher",
            Visibility = Visibility.Visible
        };

        var menu = new ContextMenu();

        var strategyMenu = new MenuItem { Header = "Стратегия" };
        strategyMenu.SubmenuOpened += (_, _) =>
        {
            if (strategyMenu.Items.Count > 0) return;
            foreach (var strategy in strategies)
            {
                var item = new MenuItem { Header = strategy.Name };
                item.Click += (_, _) => select(strategy.Id);
                strategyMenu.Items.Add(item);
            }
        };
        menu.Items.Add(strategyMenu);

        menu.Items.Add(new MenuItem
        {
            Header = "Включить / выключить",
            Command = new RelayCommand(toggle)
        });
        menu.Items.Add(new MenuItem
        {
            Header = "Проверить сейчас",
            Command = new RelayCommand(check)
        });
        menu.Items.Add(new MenuItem { Header = "Открыть", Command = new RelayCommand(open) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Выход", Command = new RelayCommand(exit) });

        _icon.ContextMenu = menu;
        _icon.TrayMouseDoubleClick += (_, _) => open();
    }

    public void Dispose() => _icon.Dispose();
}
