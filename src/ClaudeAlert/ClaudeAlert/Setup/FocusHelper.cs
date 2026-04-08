using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ClaudeAlert.Setup;

public static class FocusHelper
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

    private const int SW_RESTORE = 9;

    public static void FocusClaudeCode()
    {
        var processes = Process.GetProcesses();
        foreach (var p in processes)
        {
            try
            {
                if (p.MainWindowHandle != IntPtr.Zero &&
                    p.MainWindowTitle.Contains("Claude", StringComparison.OrdinalIgnoreCase))
                {
                    ShowWindow(p.MainWindowHandle, SW_RESTORE);
                    SetForegroundWindow(p.MainWindowHandle);
                    return;
                }
            }
            catch { }
        }
    }

    public static bool IsClaudeCodeFocused()
    {
        try
        {
            var fgWnd = GetForegroundWindow();
            if (fgWnd == IntPtr.Zero) return false;

            GetWindowThreadProcessId(fgWnd, out int pid);
            var proc = Process.GetProcessById(pid);
            return proc.MainWindowTitle.Contains("Claude", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
