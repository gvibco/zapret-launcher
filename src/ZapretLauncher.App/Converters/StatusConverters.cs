using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ZapretLauncher.App.Converters;

internal static class BrushLookup
{
    public static Brush Get(string key) =>
        Application.Current?.TryFindResource(key) as Brush ?? Brushes.Gray;
}

/// <summary>Colors a "passed/total" ratio green, amber or red.</summary>
public sealed class RatioToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text) return BrushLookup.Get("TextMuted");
        var parts = text.Split('/');
        if (parts.Length != 2
            || !int.TryParse(parts[0], out var passed)
            || !int.TryParse(parts[1], out var total)
            || total <= 0)
        {
            return BrushLookup.Get("TextMuted");
        }

        var ratio = (double)passed / total;
        return BrushLookup.Get(ratio >= 1 ? "Success" : ratio > 0 ? "Warning" : "Danger");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>Colors a strategy score ("critical/total") by quality.</summary>
public sealed class ScoreToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text) return BrushLookup.Get("TextMuted");
        var parts = text.Split('/');
        if (parts.Length != 2
            || !int.TryParse(parts[0], out var passed)
            || !int.TryParse(parts[1], out var total)
            || total <= 0)
        {
            return BrushLookup.Get("TextMuted");
        }

        var ratio = (double)passed / total;
        return BrushLookup.Get(ratio >= 1 ? "Success" : ratio >= 0.6 ? "Warning" : "Danger");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>Colors diagnostic and probe status labels.</summary>
public sealed class StatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value as string ?? "";
        return status.ToLowerInvariant() switch
        {
            "pass" or "ok" => BrushLookup.Get("Success"),
            "warn" => BrushLookup.Get("Warning"),
            "fail" => BrushLookup.Get("Danger"),
            _ => BrushLookup.Get("TextMuted")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>Translates diagnostic status codes for display.</summary>
public sealed class StatusToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value as string ?? "";
        return status.ToLowerInvariant() switch
        {
            "pass" or "ok" => "ОК",
            "warn" => "Внимание",
            "fail" => "Ошибка",
            _ => status
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>Inverse of BooleanToVisibilityConverter.</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool flag && flag ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>Shows an element only when a string has content.</summary>
public sealed class NotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string text && !string.IsNullOrWhiteSpace(text)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
