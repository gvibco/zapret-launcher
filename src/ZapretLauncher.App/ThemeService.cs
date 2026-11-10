using System.Windows;
using Microsoft.Win32;

namespace ZapretLauncher.App;

public static class ThemeService
{
    public static bool IsSystemDark()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
    }

    public static void Apply(bool dark)
    {
        var app = Application.Current;
        var existing = app.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("Themes/") == true);
        if (existing is not null) app.Resources.MergedDictionaries.Remove(existing);

        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(
                dark ? "pack://application:,,,/Themes/Dark.xaml"
                     : "pack://application:,,,/Themes/Light.xaml",
                UriKind.Absolute)
        });
    }
}
