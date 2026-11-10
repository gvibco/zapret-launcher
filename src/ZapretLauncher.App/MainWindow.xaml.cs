using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using ZapretLauncher.App.ViewModels;

namespace ZapretLauncher.App;

public partial class MainWindow : Window
{
    private bool _allowClose;

    public MainWindow() => InitializeComponent();

    public void AllowClose() => _allowClose = true;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        if (PresentationSource.FromVisual(this) is HwndSource source)
            source.AddHook(WndProc);
    }

    /// <summary>
    /// With a custom WindowChrome a maximized window would otherwise extend a few
    /// pixels past the work area, hiding the bottom of the app behind the taskbar.
    /// </summary>
    private static IntPtr WndProc(
        IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WmGetMinMaxInfo = 0x0024;
        if (message != WmGetMinMaxInfo) return IntPtr.Zero;

        var info = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor != IntPtr.Zero)
        {
            var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfo(monitor, ref monitorInfo))
            {
                var work = monitorInfo.Work;
                var bounds = monitorInfo.Monitor;
                info.MaxPosition.X = work.Left - bounds.Left;
                info.MaxPosition.Y = work.Top - bounds.Top;
                info.MaxSize.X = work.Right - work.Left;
                info.MaxSize.Y = work.Bottom - work.Top;
                info.MaxTrackSize.X = info.MaxSize.X;
                info.MaxTrackSize.Y = info.MaxSize.Y;
                Marshal.StructureToPtr(info, lParam, true);
            }
        }

        handled = true;
        return IntPtr.Zero;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose)
        {
            if (DataContext is MainViewModel { Services.Settings.CloseToTray: true })
            {
                e.Cancel = true;
                Hide();
                TrimMemory();
                return;
            }

            _allowClose = true;
            Application.Current.Shutdown();
        }
        base.OnClosing(e);
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized) TrimMemory();
    }

    private void TrimMemory()
    {
        Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false, false);
            MemoryOptimizer.TrimWorkingSet();
        }));
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

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

    private const uint MonitorDefaultToNearest = 0x00000002;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point Reserved;
        public Point MaxSize;
        public Point MaxPosition;
        public Point MinTrackSize;
        public Point MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;
    }
}
