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
    private readonly StatusBarWindow? _statusBar;

    public TrayIconManager(ClaudeStatusManager statusManager, OverlayWindow overlay, StatusBarWindow? statusBar = null)
    {
        _statusManager = statusManager;
        _overlay = overlay;
        _statusBar = statusBar;

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Text = L10n.Get("tray.tooltip.idle"),
            Icon = CreateIcon(Color.Gray)
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(L10n.Get("tray.show_hide"), null, (_, _) => ToggleWindows());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(L10n.Get("tray.exit"), null, (_, _) => System.Windows.Application.Current.Shutdown());
        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => ShowWindows();

        _statusManager.StateChanged += OnStateChanged;
    }

    private void ToggleWindows()
    {
        if (_overlay.IsVisible)
        {
            _overlay.Hide();
            _statusBar?.Hide();
        }
        else
        {
            ShowWindows();
        }
    }

    private void ShowWindows()
    {
        if (!_overlay.IsVisible) _overlay.Show();
        if (_statusBar != null && !_statusBar.IsVisible) _statusBar.Show();
    }

    private void OnStateChanged(ClaudeState oldState, ClaudeState newState)
    {
        var (color, tooltipKey) = newState switch
        {
            ClaudeState.Active => (Color.LimeGreen, "tray.tooltip.active"),
            ClaudeState.Done => (Color.DodgerBlue, "tray.tooltip.done"),
            ClaudeState.WaitingForInput => (Color.Orange, "tray.tooltip.waiting"),
            ClaudeState.Stuck => (Color.Red, "tray.tooltip.stuck"),
            ClaudeState.Error => (Color.Red, "tray.tooltip.error"),
            _ => (Color.Gray, "tray.tooltip.idle")
        };

        _notifyIcon.Icon = CreateIcon(color);
        _notifyIcon.Text = L10n.Get(tooltipKey);
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
