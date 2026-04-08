using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace ClaudeAlert.Core;

public class ClaudeStatusManager : INotifyPropertyChanged
{
    private ClaudeState _currentState = ClaudeState.Idle;
    private DateTime _lastActivityTime = DateTime.UtcNow;
    private DateTime _lastStateChangeTime = DateTime.UtcNow;
    private DateTime? _escalationStartTime;
    private DateTime _acknowledgeTime = DateTime.MinValue;
    private readonly DispatcherTimer _timer;
    private readonly AppSettings _settings;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<ClaudeState, ClaudeState>? StateChanged;
    public event Action<ClaudeEvent>? EventReceived;

    public ClaudeState CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState == value) return;
            var old = _currentState;
            _currentState = value;
            _lastStateChangeTime = DateTime.UtcNow;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(ElapsedText));
            StateChanged?.Invoke(old, value);
        }
    }

    public bool IsEscalating => _escalationStartTime.HasValue;

    public TimeSpan EscalationElapsed => _escalationStartTime.HasValue
        ? DateTime.UtcNow - _escalationStartTime.Value
        : TimeSpan.Zero;

    public string StatusText => CurrentState switch
    {
        ClaudeState.Idle => L10n.Get("status.idle"),
        ClaudeState.Active => L10n.Get("status.active"),
        ClaudeState.Done => L10n.Get("status.done"),
        ClaudeState.WaitingForInput => L10n.Get("status.waiting"),
        ClaudeState.Stuck => L10n.Get("status.stuck"),
        ClaudeState.Error => L10n.Get("status.error"),
        ClaudeState.Acknowledged => L10n.Get("status.acknowledged"),
        _ => ""
    };

    public string ElapsedText
    {
        get
        {
            if (CurrentState is ClaudeState.Idle or ClaudeState.Acknowledged)
                return "";

            var elapsed = DateTime.UtcNow - _lastStateChangeTime;
            var suffix = CurrentState == ClaudeState.Active
                ? L10n.Get("elapsed.suffix.active")
                : L10n.Get("elapsed.suffix.idle");
            return FormatElapsed(elapsed, suffix);
        }
    }

    private static string FormatElapsed(TimeSpan ts, string suffix)
    {
        if (ts.TotalSeconds < 60) return L10n.Get("elapsed.seconds", (int)ts.TotalSeconds) + suffix;
        if (ts.TotalMinutes < 60) return L10n.Get("elapsed.minutes", (int)ts.TotalMinutes) + suffix;
        return L10n.Get("elapsed.hours", (int)ts.TotalHours) + suffix;
    }

    public ClaudeStatusManager(AppSettings settings)
    {
        _settings = settings;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTimerTick;
        _timer.Start();

        L10n.LanguageChanged += () =>
        {
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(ElapsedText));
        };
    }

    public void ProcessEvent(ClaudeEvent evt)
    {
        _lastActivityTime = evt.Timestamp;
        EventReceived?.Invoke(evt);

        // After acknowledge, ignore ALL events for a short cooldown
        if ((DateTime.UtcNow - _acknowledgeTime).TotalSeconds < 5)
            return;

        switch (evt.Type)
        {
            case "tool_use":
                StopEscalation();
                CurrentState = ClaudeState.Active;
                break;
            case "stop":
                CurrentState = ClaudeState.Done;
                // Only alert if user is NOT looking at Claude Code
                if (!Setup.FocusHelper.IsClaudeCodeFocused())
                    StartEscalation();
                break;
            case "permission_prompt":
                CurrentState = ClaudeState.WaitingForInput;
                if (!Setup.FocusHelper.IsClaudeCodeFocused())
                    StartEscalation();
                break;
            case "idle_prompt":
                CurrentState = ClaudeState.WaitingForInput;
                if (!Setup.FocusHelper.IsClaudeCodeFocused())
                    StartEscalation();
                break;
            case "user_active":
                if (IsEscalating)
                    Acknowledge();
                break;
            case "error":
                StartEscalation();
                CurrentState = ClaudeState.Error;
                break;
        }
    }

    public void Acknowledge()
    {
        StopEscalation();
        _acknowledgeTime = DateTime.UtcNow;
        CurrentState = ClaudeState.Acknowledged;
    }

    private void StartEscalation()
    {
        _escalationStartTime = DateTime.UtcNow;
    }

    private void StopEscalation()
    {
        _escalationStartTime = null;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(ElapsedText));
        OnPropertyChanged(nameof(EscalationElapsed));

        // Stuck detection: Active for too long without events
        if (CurrentState == ClaudeState.Active)
        {
            var sinceLastActivity = DateTime.UtcNow - _lastActivityTime;
            if (sinceLastActivity.TotalSeconds >= _settings.StuckThresholdSeconds)
            {
                StartEscalation();
                CurrentState = ClaudeState.Stuck;
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
