using System.Windows;
using System.Windows.Threading;
using ClaudeAlert.Core;

namespace ClaudeAlert.Physics;

public enum EscalationLevel
{
    None,
    Jump,
    Roll,
    Bounce
}

public class EscalationController
{
    private readonly PhysicsBody _body;
    private readonly PhysicsEngine _engine;
    private readonly ClaudeStatusManager _statusManager;
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _actionTimer;
    private readonly Random _random = new();
    private EscalationLevel _currentLevel = EscalationLevel.None;
    private double _rollDirection = 1;
    private DateTime _lastActionTime = DateTime.MinValue;

    public event Action<EscalationLevel>? LevelChanged;
    public EscalationLevel CurrentLevel => _currentLevel;

    public EscalationController(PhysicsBody body, PhysicsEngine engine,
        ClaudeStatusManager statusManager, AppSettings settings)
    {
        _body = body;
        _engine = engine;
        _statusManager = statusManager;
        _settings = settings;

        _actionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _actionTimer.Tick += OnActionTick;
        _actionTimer.Start();
    }

    private void OnActionTick(object? sender, EventArgs e)
    {
        if (!_statusManager.IsEscalating)
        {
            if (_currentLevel != EscalationLevel.None)
            {
                _currentLevel = EscalationLevel.None;
                LevelChanged?.Invoke(_currentLevel);
            }
            return;
        }

        var elapsed = _statusManager.EscalationElapsed.TotalSeconds;
        var newLevel = elapsed switch
        {
            >= 180 => EscalationLevel.Bounce, // 3 min -> screen bounce
            >= 60 => EscalationLevel.Roll,     // 1 min -> roll on taskbar
            >= 30 => EscalationLevel.Jump,     // 30s -> jump in place
            _ => EscalationLevel.None
        };

        if (newLevel != _currentLevel)
        {
            _currentLevel = newLevel;
            LevelChanged?.Invoke(_currentLevel);
        }

        // Throttle actions - don't fire every 100ms
        var now = DateTime.UtcNow;
        var cooldown = _currentLevel switch
        {
            EscalationLevel.Jump => TimeSpan.FromSeconds(2.5),
            EscalationLevel.Roll => TimeSpan.FromSeconds(3),
            EscalationLevel.Bounce => TimeSpan.FromSeconds(2),
            _ => TimeSpan.FromSeconds(1)
        };
        if (now - _lastActionTime < cooldown) return;

        switch (_currentLevel)
        {
            case EscalationLevel.Jump:
                DoJump();
                break;
            case EscalationLevel.Roll:
                DoRoll();
                break;
            case EscalationLevel.Bounce:
                DoBounce();
                break;
        }
        _lastActionTime = now;
    }

    private void DoJump()
    {
        if (_body.IsStatic || Math.Abs(_body.Velocity.Y) < 1)
        {
            _body.ApplyImpulse(new Vector(0, -350 - _random.Next(100)));
            _engine.Start();
        }
    }

    private void DoRoll()
    {
        // Roll along taskbar (use current monitor bounds)
        if (_body.IsStatic || Math.Abs(_body.Velocity.X) < 10)
        {
            // Use engine's current screen bounds (already DPI-corrected)
            var screenLeft = _engine.ScreenLeft;
            var screenRight = _engine.ScreenRight;

            if (_body.Position.X <= screenLeft + 10) _rollDirection = 1;
            else if (_body.Right >= screenRight - 10) _rollDirection = -1;

            _body.ApplyImpulse(new Vector(_rollDirection * 200, -100));
            _body.AngularVelocity = _rollDirection * 360;
            _engine.Start();
        }
    }

    private void DoBounce()
    {
        // Full screen bounce - chaotic mode
        if (_body.IsStatic || _body.Velocity.Length < 100)
        {
            var angle = _random.NextDouble() * Math.PI * 2;
            var speed = 800 + _random.Next(400);
            _body.ApplyImpulse(new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed - 600));
            _body.AngularVelocity = (_random.NextDouble() - 0.5) * 1440;
            _body.Gravity = 300;
            _body.BounceFactor = 0.92;
            _engine.Start();
        }
    }

    public void Reset()
    {
        _currentLevel = EscalationLevel.None;
        _body.Gravity = 980;
        _body.BounceFactor = 0.5;
        _body.MakeStatic();
        _body.Rotation = 0;
        _body.ScaleX = 1;
        _body.ScaleY = 1;
        LevelChanged?.Invoke(_currentLevel);
    }

    public void MiniJump()
    {
        // Small playful jump on click (non-escalation)
        if (_body.IsStatic)
        {
            _body.ApplyImpulse(new Vector((_random.NextDouble() - 0.5) * 60, -200));
            _engine.Start();
        }
    }
}
