using System.Windows.Forms;

namespace ClaudeAlert.Notifications;

public class ToastNotifier
{
    private readonly NotifyIcon _notifyIcon;

    public ToastNotifier()
    {
        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Icon = System.Drawing.SystemIcons.Information,
            BalloonTipIcon = ToolTipIcon.Info
        };
    }

    public void Show(string title, string message)
    {
        try
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.ShowBalloonTip(3000);
        }
        catch { }
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
