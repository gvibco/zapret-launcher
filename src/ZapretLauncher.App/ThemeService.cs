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
        var dictionaries = app.Resources.MergedDictionaries;
        var existing = dictionaries
            .Where(d => d.Source is { } source && IsThemePalette(source))
            .ToList();
        foreach (var dictionary in existing) dictionaries.Remove(dictionary);

        dictionaries.Insert(0, new ResourceDictionary
        {
            Source = new Uri(
                dark ? "pack://application:,,,/Themes/Dark.xaml"
                     : "pack://application:,,,/Themes/Light.xaml",
                UriKind.Absolute)
        });
    }

    private static bool IsThemePalette(Uri source) =>
        source.OriginalString.EndsWith("Dark.xaml", StringComparison.OrdinalIgnoreCase)
        || source.OriginalString.EndsWith("Light.xaml", StringComparison.OrdinalIgnoreCase);
}
