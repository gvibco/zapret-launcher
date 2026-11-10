using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZapretLauncher.Core.Autostart;
using ZapretLauncher.Core.Diagnostics;
using ZapretLauncher.Core.Settings;

namespace ZapretLauncher.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AppServices _services;

    public SettingsViewModel(AppServices services)
    {
        _services = services;
        TargetsText = string.Join(Environment.NewLine, services.Settings.Targets);
    }

    public AppSettings Settings => _services.Settings;
    public string EngineVersion => _services.Engine.CurrentVersion;
    public string DataRoot => _services.DataRoot;

    [ObservableProperty] private string updateStatus = "";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string targetsText = "";

    [RelayCommand]
    private void Save()
    {
        _services.Settings.Targets = TargetsText
            .Split(new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        _services.SaveSettings();
        UpdateStatus = "Настройки сохранены.";
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var info = await _services.Updates.GetLatestAsync();
            if (info is null)
            {
                UpdateStatus = "Не удалось получить информацию об обновлении.";
                return;
            }
            UpdateStatus = info.Version == _services.Engine.CurrentVersion
                ? "Установлена последняя версия."
                : $"Доступна версия {info.Version}.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task InstallUpdateAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            if (_services.Runner.IsRunning) await _services.Runner.StopAsync();
            var info = await _services.Updates.GetLatestAsync();
            if (info is null)
            {
                UpdateStatus = "Обновление не найдено.";
                return;
            }
            await _services.Updates.DownloadAndInstallAsync(info);
            UpdateStatus = $"Движок обновлён до {info.Version}. Перезапустите приложение.";
        }
        catch (Exception ex)
        {
            UpdateStatus = "Ошибка обновления: " + ex.Message;
            _services.Log.Write(ex.ToString());
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApplyAutostartAsync()
    {
        try
        {
            var service = new AutostartService(new ProcessRunner());
            var exe = Environment.ProcessPath ?? throw new InvalidOperationException("Process path is unknown.");
            if (Settings.Autostart) await service.EnableAsync(exe);
            else await service.DisableAsync();
            _services.SaveSettings();
            UpdateStatus = Settings.Autostart
                ? "Автозапуск включён через Планировщик заданий."
                : "Автозапуск выключен.";
        }
        catch (Exception ex) when (Settings.Autostart)
        {
            var exe = Environment.ProcessPath ?? "";
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run");
            key?.SetValue("ZapretLauncher", $"\"{exe}\"");
            UpdateStatus =
                "Задача Планировщика не создана, включён автозапуск через реестр (возможен запрос UAC). "
                + ex.Message;
        }
        catch (Exception ex)
        {
            UpdateStatus = "Ошибка автозапуска: " + ex.Message;
        }
    }

    [RelayCommand]
    private void OpenDataFolder() =>
        Process.Start(new ProcessStartInfo("explorer.exe", _services.DataRoot) { UseShellExecute = true });

    [RelayCommand]
    private void ResetSettings()
    {
        _services.ResetSettings();
        OnPropertyChanged(nameof(Settings));
        UpdateStatus = "Настройки сброшены. Перезапустите приложение.";
    }
}
