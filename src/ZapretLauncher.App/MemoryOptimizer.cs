using System.Runtime.InteropServices;

namespace ZapretLauncher.App;

internal static class MemoryOptimizer
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll")]
    private static extern bool SetProcessWorkingSetSize(IntPtr process, IntPtr minimum, IntPtr maximum);

    /// <summary>Returns unused pages to the OS after the window is hidden or minimized.</summary>
    public static void TrimWorkingSet()
    {
        try
        {
            SetProcessWorkingSetSize(GetCurrentProcess(), new IntPtr(-1), new IntPtr(-1));
        }
        catch
        {
            // Trimming is best-effort; never fail the UI because of it.
        }
    }
}
