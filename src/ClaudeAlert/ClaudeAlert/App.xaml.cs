using System.Threading;
using System.Windows;
using System.Windows.Threading;
using ClaudeAlert.Core;
using ClaudeAlert.EventSources;
using ClaudeAlert.Notifications;
using ClaudeAlert.Setup;
using ClaudeAlert.TrayIcon;
using ClaudeAlert.Views;

namespace ClaudeAlert;

public partial class App : Application
{
    private Mutex? _mutex;
    private CancellationTokenSource? _cts;
    private HookHttpListener? _listener;
    private LogFileWatcher? _logWatcher;
    private SessionFileMonitor? _sessionMonitor;
    private TrayIconManager? _trayManager;
    private OverlayWindow? _overlay;
    private StatusBarWindow? _statusBar;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        // Single instance check
        _mutex = new Mutex(true, "ClaudeAlert_SingleInstance", out bool created);
        if (!created)
        {
            MessageBox.Show(L10n.Get("app.already_running"), "ClaudeAlert");
            Shutdown();
            return;
        }

        var settings = AppSettings.Load();
        settings.Save(); // ensure dirs exist

        // Initialize localization
        L10n.SetLanguage(settings.Language);

        // Auto-configure Claude Code hooks
        if (!HookConfigurator.IsConfigured(settings.Port))
        {
            HookConfigurator.EnsureHooksConfigured(settings.Port);
        }

        var statusManager = new ClaudeStatusManager(settings);

        // HTTP listener
        _cts = new CancellationTokenSource();
        _listener = new HookHttpListener(settings.Port);
        _listener.OnEvent += evt =>
        {
            Dispatcher.InvokeAsync(() => statusManager.ProcessEvent(evt));
        };
        _ = _listener.StartAsync(_cts.Token);

        // Overlay window (character only)
        _overlay = new OverlayWindow(statusManager, settings);
        _overlay.Show();

        // Status bar window (fixed position text)
        _statusBar = new StatusBarWindow(statusManager, settings);
        _statusBar.Show();

        // Link overlay to status bar for settings sync
        _overlay.StatusBar = _statusBar;

        // Toast notifications
        var toastNotifier = new ToastNotifier();
        statusManager.StateChanged += (oldState, newState) =>
        {
            switch (newState)
            {
                case ClaudeState.Done:
                    toastNotifier.Show(L10n.Get("toast.done.title"), L10n.Get("toast.done.body"));
                    break;
                case ClaudeState.WaitingForInput:
                    toastNotifier.Show(L10n.Get("toast.waiting.title"), L10n.Get("toast.waiting.body"));
                    break;
                case ClaudeState.Stuck:
                    toastNotifier.Show(L10n.Get("toast.stuck.title"), L10n.Get("toast.stuck.body"));
                    break;
                case ClaudeState.Error:
                    toastNotifier.Show(L10n.Get("toast.error.title"), L10n.Get("toast.error.body"));
                    break;
            }
        };

        // Sound
        var soundManager = new SoundManager(settings);
        _overlay.Escalation.LevelChanged += level =>
        {
            if (level == Physics.EscalationLevel.Jump)
                soundManager.PlayAlert();
            else if (level == Physics.EscalationLevel.Bounce)
                soundManager.PlayUrgent();
        };

        // Log file watcher (error detection fallback)
        _logWatcher = new LogFileWatcher();
        _logWatcher.OnEvent += evt =>
        {
            Dispatcher.InvokeAsync(() => statusManager.ProcessEvent(evt));
        };
        _ = _logWatcher.StartAsync(_cts.Token);

        // Session file monitor
        _sessionMonitor = new SessionFileMonitor();
        _ = _sessionMonitor.StartAsync(_cts.Token);

        // System tray
        _trayManager = new TrayIconManager(statusManager, _overlay, _statusBar);

        // Auto-acknowledge when Claude Code window is focused
        var focusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        focusTimer.Tick += (_, _) =>
        {
            if (statusManager.IsEscalating && FocusHelper.IsClaudeCodeFocused())
            {
                statusManager.Acknowledge();
            }
        };
        focusTimer.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _cts?.Cancel();
        _listener?.Dispose();
        _logWatcher?.Dispose();
        _sessionMonitor?.Dispose();
        _trayManager?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
