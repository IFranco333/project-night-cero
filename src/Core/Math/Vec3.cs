namespace NightCero.Core.Math;

/// <summary>
/// An immutable 3D vector in world space. X is east, Y up, Z south; units are metres.
/// </summary>
public readonly record struct Vec3(float X, float Y, float Z)
{
    public static Vec3 Zero => default;

    public static Vec3 Up => new(0, 1, 0);

    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vec3 operator -(Vec3 a) => new(-a.X, -a.Y, -a.Z);

    public static Vec3 operator *(Vec3 a, float scalar) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);

    public static Vec3 operator *(float scalar, Vec3 a) => a * scalar;

    public float Dot(Vec3 other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);

    public float Length => MathF.Sqrt(Dot(this));

    /// <summary>Horizontal distance in the XZ plane: used for movement and perception ranges.</summary>
    public float LengthXZ => MathF.Sqrt((X * X) + (Z * Z));

    public Vec3 Normalized()
    {
        var length = Length;
        return length > 0 ? this * (1f / length) : Zero;
    }

    public override string ToString() => $"({X}, {Y}, {Z})";
}
