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

    public static string GetForegroundWindowTitle()
    {
        try
        {
            var fgWnd = GetForegroundWindow();
            if (fgWnd == IntPtr.Zero) return "(none)";
            var sb = new System.Text.StringBuilder(256);
            GetWindowText(fgWnd, sb, 256);
            return sb.ToString();
        }
        catch { return "(error)"; }
    }

    public static bool IsClaudeCodeFocused()
    {
        try
        {
            var fgWnd = GetForegroundWindow();
            if (fgWnd == IntPtr.Zero) return false;

            GetWindowThreadProcessId(fgWnd, out int pid);

            // Exclude ourselves
            if (pid == Environment.ProcessId)
                return false;

            var proc = Process.GetProcessById(pid);
            var procName = proc.ProcessName.ToLowerInvariant();

            // Claude Code desktop app process
            if (procName.Contains("claude"))
                return true;

            // Terminal apps that might be running Claude Code CLI
            var isTerminal = procName is "windowsterminal" or "cmd" or "powershell"
                or "pwsh" or "conhost" or "wt";

            if (isTerminal)
            {
                // Check window title for Claude Code indicators
                var sb = new System.Text.StringBuilder(512);
                GetWindowText(fgWnd, sb, 512);
                var title = sb.ToString();

                // Match "claude" as a command, not as part of a file path
                // e.g. "claude - WindowsTerminal" or "claude code" but NOT "C:\...\Claude\..."
                if (title.StartsWith("claude", StringComparison.OrdinalIgnoreCase) ||
                    title.Contains(" claude", StringComparison.OrdinalIgnoreCase) ||
                    title.Contains("Claude Code", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
