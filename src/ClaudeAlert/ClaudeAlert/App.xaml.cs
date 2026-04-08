using System.Threading;
using System.Windows;
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

    private void OnStartup(object sender, StartupEventArgs e)
    {
        // Single instance check
        _mutex = new Mutex(true, "ClaudeAlert_SingleInstance", out bool created);
        if (!created)
        {
            MessageBox.Show("ClaudeAlert가 이미 실행 중입니다.", "ClaudeAlert");
            Shutdown();
            return;
        }

        var settings = AppSettings.Load();
        settings.Save(); // ensure dirs exist

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

        // Overlay window
        _overlay = new OverlayWindow(statusManager, settings);
        _overlay.Show();

        // Toast notifications
        var toastNotifier = new ToastNotifier();
        statusManager.StateChanged += (oldState, newState) =>
        {
            switch (newState)
            {
                case ClaudeState.Done:
                    toastNotifier.Show("Claude 응답 완료", "Claude가 작업을 마쳤습니다.");
                    break;
                case ClaudeState.WaitingForInput:
                    toastNotifier.Show("입력 필요", "Claude가 사용자의 입력을 기다리고 있습니다.");
                    break;
                case ClaudeState.Stuck:
                    toastNotifier.Show("Claude 멈춤", "Claude가 오랫동안 응답하지 않고 있습니다.");
                    break;
                case ClaudeState.Error:
                    toastNotifier.Show("오류 발생", "Claude에서 오류가 감지되었습니다.");
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
        _trayManager = new TrayIconManager(statusManager, _overlay);
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
