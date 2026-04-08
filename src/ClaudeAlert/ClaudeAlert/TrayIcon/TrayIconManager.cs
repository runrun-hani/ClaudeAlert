using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using ClaudeAlert.Core;
using ClaudeAlert.Views;

namespace ClaudeAlert.TrayIcon;

public class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ClaudeStatusManager _statusManager;
    private readonly OverlayWindow _overlay;

    public TrayIconManager(ClaudeStatusManager statusManager, OverlayWindow overlay)
    {
        _statusManager = statusManager;
        _overlay = overlay;

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Text = "ClaudeAlert - 대기",
            Icon = CreateIcon(Color.Gray)
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("보이기/숨기기", null, (_, _) =>
        {
            if (_overlay.IsVisible) _overlay.Hide();
            else _overlay.Show();
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("종료", null, (_, _) => System.Windows.Application.Current.Shutdown());
        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) =>
        {
            if (!_overlay.IsVisible) _overlay.Show();
        };

        _statusManager.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(ClaudeState oldState, ClaudeState newState)
    {
        var (color, text) = newState switch
        {
            ClaudeState.Active => (Color.LimeGreen, "작업 중"),
            ClaudeState.Done => (Color.DodgerBlue, "완료"),
            ClaudeState.WaitingForInput => (Color.Orange, "입력 대기"),
            ClaudeState.Stuck => (Color.Red, "멈춤"),
            ClaudeState.Error => (Color.Red, "오류"),
            _ => (Color.Gray, "대기")
        };

        _notifyIcon.Icon = CreateIcon(color);
        _notifyIcon.Text = $"ClaudeAlert - {text}";
    }

    private static Icon CreateIcon(Color color)
    {
        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, 14, 14);
        g.DrawEllipse(Pens.White, 1, 1, 14, 14);
        return Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
