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

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

    public static bool IsClaudeCodeFocused()
    {
        try
        {
            var fgWnd = GetForegroundWindow();
            if (fgWnd == IntPtr.Zero) return false;

            // Get the actual foreground window title (more reliable than process title)
            var sb = new System.Text.StringBuilder(512);
            GetWindowText(fgWnd, sb, 512);
            var title = sb.ToString();

            // Match Claude Code desktop app, terminal running claude, etc.
            if (title.Contains("Claude", StringComparison.OrdinalIgnoreCase))
                return true;

            // Also check process name for claude-related processes
            GetWindowThreadProcessId(fgWnd, out int pid);
            var proc = Process.GetProcessById(pid);
            var procName = proc.ProcessName;
            return procName.Contains("claude", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
