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
            _ when elapsed >= _settings.EscalationBounceSeconds => EscalationLevel.Bounce,
            _ when elapsed >= _settings.EscalationRollSeconds => EscalationLevel.Roll,
            _ when elapsed >= _settings.EscalationJumpSeconds => EscalationLevel.Jump,
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
            EscalationLevel.Bounce => TimeSpan.FromMilliseconds(100),
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

            _body.ApplyImpulse(new Vector(_rollDirection * 400, -150));
            _body.AngularVelocity = _rollDirection * 540;
            _engine.Start();
        }
    }

    private const double BounceAccelForce = 200;
    private const double BounceMaxSpeed = 1200;

    private void DoBounce()
    {
        _body.Gravity = 300;
        _body.BounceFactor = 0.92;
        _engine.Start();

        if (_body.IsStatic || _body.Velocity.Length < 10)
        {
            // Initial kick in random direction
            var angle = _random.NextDouble() * Math.PI * 2;
            _body.ApplyImpulse(new Vector(Math.Cos(angle) * 400, Math.Sin(angle) * 400 - 300));
            _body.AngularVelocity = (_random.NextDouble() - 0.5) * 720;
        }
        else if (_body.Velocity.Length < BounceMaxSpeed)
        {
            // Accelerate in current movement direction
            var dir = _body.Velocity;
            dir.Normalize();
            _body.ApplyImpulse(dir * BounceAccelForce);
        }
    }

    public void Reset()
    {
        _currentLevel = EscalationLevel.None;
        _body.Gravity = 980;
        _body.BounceFactor = 0.5;
        // Stop in place — don't teleport to ground
        _body.Velocity = new Vector(0, 0);
        _body.AngularVelocity = 0;
        _body.IsStatic = false; // let gravity bring it down naturally
        _body.ScaleX = 1;
        _body.ScaleY = 1;
        _engine.Start(); // engine will stop when body reaches ground
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
