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
        ClaudeState.Idle => "대기",
        ClaudeState.Active => "작업 중",
        ClaudeState.Done => "완료",
        ClaudeState.WaitingForInput => "입력 대기",
        ClaudeState.Stuck => "멈춤",
        ClaudeState.Error => "오류",
        ClaudeState.Acknowledged => "확인됨",
        _ => ""
    };

    public string ElapsedText
    {
        get
        {
            var elapsed = DateTime.UtcNow - _lastStateChangeTime;
            return CurrentState == ClaudeState.Active
                ? FormatElapsed(elapsed, "째")
                : FormatElapsed(elapsed, " 지남");
        }
    }

    private static string FormatElapsed(TimeSpan ts, string suffix)
    {
        if (ts.TotalSeconds < 60) return $"{(int)ts.TotalSeconds}초{suffix}";
        if (ts.TotalMinutes < 60) return $"{(int)ts.TotalMinutes}분{suffix}";
        return $"{(int)ts.TotalHours}시간{suffix}";
    }

    public ClaudeStatusManager(AppSettings settings)
    {
        _settings = settings;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    public void ProcessEvent(ClaudeEvent evt)
    {
        _lastActivityTime = evt.Timestamp;
        EventReceived?.Invoke(evt);

        switch (evt.Type)
        {
            case "tool_use":
                StopEscalation();
                CurrentState = ClaudeState.Active;
                break;
            case "stop":
                StartEscalation();
                CurrentState = ClaudeState.Done;
                break;
            case "permission_prompt":
                StartEscalation();
                CurrentState = ClaudeState.WaitingForInput;
                break;
            case "idle_prompt":
                StartEscalation();
                CurrentState = ClaudeState.WaitingForInput;
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
