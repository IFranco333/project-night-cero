namespace NightCero.Core.Math;

/// <summary>A 2D vector on the ground plane: X east, Y south (the XZ plane of <see cref="Vec3"/>).</summary>
public readonly record struct Vec2(float X, float Y)
{
    public static Vec2 Zero => default;

    /// <summary>North, the direction a player faces at yaw 0.</summary>
    public static Vec2 Forward => new(0, -1);

    public static Vec2 Right => new(1, 0);

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);

    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);

    public static Vec2 operator *(Vec2 a, float scalar) => new(a.X * scalar, a.Y * scalar);

    public static Vec2 operator *(float scalar, Vec2 a) => a * scalar;

    public float Length => MathF.Sqrt((X * X) + (Y * Y));

    public Vec2 Normalized()
    {
        var length = Length;
        return length > 0 ? this * (1f / length) : Zero;
    }

    /// <summary>Lifts the vector onto the XZ plane of world space.</summary>
    public Vec3 ToXZ() => new(X, 0, Y);

    public override string ToString() => $"({X}, {Y})";
}
