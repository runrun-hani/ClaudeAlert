using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace ClaudeAlert.Physics;

public class PhysicsEngine
{
    private readonly PhysicsBody _body;
    private double _groundY;
    private double _screenLeft;
    private double _screenTop;
    private double _screenRight;
    private double _screenBottom;
    private double _dpiScale = 1.0;
    private bool _running;
    private DateTime _lastFrame;

    public event Action? Updated;

    public PhysicsBody Body => _body;

    public PhysicsEngine(PhysicsBody body)
    {
        _body = body;
        // Detect DPI scale: physical pixels / WPF logical units
        using var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
        _dpiScale = g.DpiX / 96.0;

        UpdateScreenBounds();
    }

    public void UpdateScreenBounds()
    {
        // Get the monitor that contains the body's current center position
        // Convert WPF logical coords to physical pixels for Screen.FromPoint
        var centerX = (int)((_body.Position.X + _body.Width / 2) * _dpiScale);
        var centerY = (int)((_body.Position.Y + _body.Height / 2) * _dpiScale);
        var screen = Screen.FromPoint(new System.Drawing.Point(centerX, centerY));
        var wa = screen.WorkingArea; // physical pixels, excludes taskbar

        // Convert physical pixels back to WPF logical units
        _screenLeft = wa.Left / _dpiScale;
        _screenTop = wa.Top / _dpiScale;
        _screenRight = wa.Right / _dpiScale;
        _screenBottom = wa.Bottom / _dpiScale;
        _groundY = _screenBottom - _body.Height;
    }

    public double GroundY => _groundY;
    public double ScreenLeft => _screenLeft;
    public double ScreenRight => _screenRight;

    public void Start()
    {
        if (_running) return;
        _running = true;
        _lastFrame = DateTime.UtcNow;
        CompositionTarget.Rendering += OnRendering;
    }

    public void Stop()
    {
        _running = false;
        CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        var dt = (now - _lastFrame).TotalSeconds;
        _lastFrame = now;

        if (dt > 0.1) dt = 0.1;
        if (_body.IsStatic || _body.IsDragging) return;

        Step(dt);
        Updated?.Invoke();
    }

    private void Step(double dt)
    {
        // Apply gravity
        var vel = _body.Velocity;
        vel.Y += _body.Gravity * dt;
        _body.Velocity = vel;

        // Update position
        var pos = _body.Position;
        pos.X += _body.Velocity.X * dt;
        pos.Y += _body.Velocity.Y * dt;

        // Rotation
        _body.Rotation += _body.AngularVelocity * dt;

        // Recalculate screen bounds based on current position (handles monitor transitions)
        var centerX = (int)((pos.X + _body.Width / 2) * _dpiScale);
        var centerY = (int)((pos.Y + _body.Height / 2) * _dpiScale);
        var screen = Screen.FromPoint(new System.Drawing.Point(centerX, centerY));
        var wa = screen.WorkingArea;
        _screenLeft = wa.Left / _dpiScale;
        _screenTop = wa.Top / _dpiScale;
        _screenRight = wa.Right / _dpiScale;
        _screenBottom = wa.Bottom / _dpiScale;
        _groundY = _screenBottom - _body.Height;

        // Ground collision
        if (pos.Y >= _groundY)
        {
            pos.Y = _groundY;
            var vy = _body.Velocity.Y;
            if (Math.Abs(vy) < 30)
            {
                _body.Velocity = new Vector(_body.Velocity.X * _body.Friction, 0);
                _body.AngularVelocity *= 0.8;

                if (Math.Abs(_body.Velocity.X) < 5 && Math.Abs(_body.AngularVelocity) < 1)
                {
                    _body.MakeStatic();
                    _body.Rotation = 0;
                    _body.ScaleX = 1;
                    _body.ScaleY = 1;
                }
            }
            else
            {
                _body.Velocity = new Vector(
                    _body.Velocity.X * _body.Friction,
                    -vy * _body.BounceFactor);

                var squash = Math.Min(0.3, Math.Abs(vy) / 2000.0);
                _body.ScaleY = 1.0 - squash;
                _body.ScaleX = 1.0 + squash * 0.5;
            }
        }

        // Wall collisions (per-monitor bounds)
        if (pos.X <= _screenLeft)
        {
            pos.X = _screenLeft;
            _body.Velocity = new Vector(-_body.Velocity.X * _body.BounceFactor, _body.Velocity.Y);
        }
        else if (pos.X + _body.Width >= _screenRight)
        {
            pos.X = _screenRight - _body.Width;
            _body.Velocity = new Vector(-_body.Velocity.X * _body.BounceFactor, _body.Velocity.Y);
        }

        // Ceiling collision
        if (pos.Y <= _screenTop)
        {
            pos.Y = _screenTop;
            _body.Velocity = new Vector(_body.Velocity.X, -_body.Velocity.Y * _body.BounceFactor);
        }

        _body.Position = pos;

        // Recover squash
        var recovery = _body.SquashRecoverySpeed * dt;
        _body.ScaleY += (1.0 - _body.ScaleY) * Math.Min(recovery, 1.0);
        _body.ScaleX += (1.0 - _body.ScaleX) * Math.Min(recovery, 1.0);
        if (Math.Abs(_body.ScaleY - 1.0) < 0.01) _body.ScaleY = 1.0;
        if (Math.Abs(_body.ScaleX - 1.0) < 0.01) _body.ScaleX = 1.0;
    }
}
