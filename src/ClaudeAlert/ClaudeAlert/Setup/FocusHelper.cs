using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ClaudeAlert.Setup;

public static class FocusHelper
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    public static void FocusClaudeCode()
    {
        // Try to find Claude Code window (Electron-based, title contains "Claude")
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
}
