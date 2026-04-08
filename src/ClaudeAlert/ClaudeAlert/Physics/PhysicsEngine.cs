using System.Windows;
using System.Windows.Media;

namespace ClaudeAlert.Physics;

public class PhysicsEngine
{
    private readonly PhysicsBody _body;
    private double _groundY;
    private double _screenWidth;
    private double _screenHeight;
    private bool _running;
    private DateTime _lastFrame;

    public event Action? Updated;

    public PhysicsBody Body => _body;

    public PhysicsEngine(PhysicsBody body)
    {
        _body = body;
        UpdateScreenBounds();
    }

    public void UpdateScreenBounds()
    {
        _screenWidth = SystemParameters.PrimaryScreenWidth;
        _screenHeight = SystemParameters.PrimaryScreenHeight;
        // Ground = top of taskbar (approx 48px from bottom)
        _groundY = _screenHeight - 48 - _body.Height;
    }

    public double GroundY => _groundY;

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

        if (dt > 0.1) dt = 0.1; // clamp large gaps
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

        // Ground collision
        if (pos.Y >= _groundY)
        {
            pos.Y = _groundY;
            var vy = _body.Velocity.Y;
            if (Math.Abs(vy) < 30)
            {
                // Come to rest
                _body.Velocity = new Vector(_body.Velocity.X * _body.Friction, 0);
                _body.AngularVelocity *= 0.8;

                // Stop if nearly still
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
                // Bounce
                _body.Velocity = new Vector(
                    _body.Velocity.X * _body.Friction,
                    -vy * _body.BounceFactor);

                // Squash on impact
                var squash = Math.Min(0.3, Math.Abs(vy) / 2000.0);
                _body.ScaleY = 1.0 - squash;
                _body.ScaleX = 1.0 + squash * 0.5;
            }
        }

        // Wall collisions
        if (pos.X <= 0)
        {
            pos.X = 0;
            _body.Velocity = new Vector(-_body.Velocity.X * _body.BounceFactor, _body.Velocity.Y);
        }
        else if (pos.X + _body.Width >= _screenWidth)
        {
            pos.X = _screenWidth - _body.Width;
            _body.Velocity = new Vector(-_body.Velocity.X * _body.BounceFactor, _body.Velocity.Y);
        }

        // Ceiling collision
        if (pos.Y <= 0)
        {
            pos.Y = 0;
            _body.Velocity = new Vector(_body.Velocity.X, -_body.Velocity.Y * _body.BounceFactor);
        }

        _body.Position = pos;

        // Recover squash - lerp back to 1.0
        var recovery = _body.SquashRecoverySpeed * dt;
        _body.ScaleY += (1.0 - _body.ScaleY) * Math.Min(recovery, 1.0);
        _body.ScaleX += (1.0 - _body.ScaleX) * Math.Min(recovery, 1.0);
        // Snap to 1.0 when close enough
        if (Math.Abs(_body.ScaleY - 1.0) < 0.01) _body.ScaleY = 1.0;
        if (Math.Abs(_body.ScaleX - 1.0) < 0.01) _body.ScaleX = 1.0;
    }
}
