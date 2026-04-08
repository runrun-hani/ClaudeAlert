using System.Windows;

namespace ClaudeAlert.Physics;

public class PhysicsBody
{
    public Point Position { get; set; }
    public Vector Velocity { get; set; }
    public double Rotation { get; set; }
    public double AngularVelocity { get; set; }
    public double ScaleX { get; set; } = 1.0;
    public double ScaleY { get; set; } = 1.0;
    public double Width { get; set; } = 64;
    public double Height { get; set; } = 64;
    public bool IsStatic { get; set; } = true;
    public bool IsDragging { get; set; }

    public double Gravity { get; set; } = 980;
    public double BounceFactor { get; set; } = 0.5;
    public double Friction { get; set; } = 0.95;
    public double SquashRecoverySpeed { get; set; } = 8.0;

    public Rect Bounds => new(Position.X, Position.Y, Width, Height);

    public double Bottom => Position.Y + Height;
    public double Right => Position.X + Width;
    public double CenterX => Position.X + Width / 2;
    public double CenterY => Position.Y + Height / 2;

    public void ApplyImpulse(Vector impulse)
    {
        Velocity += impulse;
        IsStatic = false;
    }

    public void MakeStatic()
    {
        Velocity = new Vector(0, 0);
        AngularVelocity = 0;
        IsStatic = true;
    }
}
