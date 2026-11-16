namespace NightCero.Core.Math;

/// <summary>
/// An axis-aligned rectangle on the ground plane. Coordinates follow <see cref="Vec2"/>: X east,
/// Y south; <see cref="Width"/> spans X and <see cref="Height"/> spans Y.
/// </summary>
public readonly record struct Rect2(float MinX, float MinY, float MaxX, float MaxY)
{
    /// <summary>Builds a rectangle from its centre and size.</summary>
    public static Rect2 FromCentre(Vec2 centre, float width, float height)
    {
        var halfWidth = MathF.Max(width, 0f) / 2f;
        var halfHeight = MathF.Max(height, 0f) / 2f;
        return new Rect2(
            centre.X - halfWidth,
            centre.Y - halfHeight,
            centre.X + halfWidth,
            centre.Y + halfHeight);
    }

    /// <summary>The size along X.</summary>
    public float Width => MaxX - MinX;

    /// <summary>The size along Y (south in world space).</summary>
    public float Height => MaxY - MinY;

    /// <summary>The centre point of the rectangle.</summary>
    public Vec2 Centre => new((MinX + MaxX) / 2f, (MinY + MaxY) / 2f);

    /// <summary>Whether the point lies inside or on the boundary.</summary>
    public bool Contains(Vec2 point) =>
        point.X >= MinX && point.X <= MaxX && point.Y >= MinY && point.Y <= MaxY;

    /// <summary>Whether the two rectangles share interior area. Touching edges do not count.</summary>
    public bool Intersects(Rect2 other) =>
        MinX < other.MaxX && MaxX > other.MinX && MinY < other.MaxY && MaxY > other.MinY;

    /// <summary>Grows the rectangle by <paramref name="amount"/> on every side; negative shrinks it.</summary>
    public Rect2 Expand(float amount) =>
        new(MinX - amount, MinY - amount, MaxX + amount, MaxY + amount);

    /// <summary>
    /// Whether the segment <paramref name="from"/>–<paramref name="to"/> touches the rectangle,
    /// including when it only grazes an edge (Liang–Barsky clipping).
    /// </summary>
    public bool IntersectsSegment(Vec2 from, Vec2 to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var t0 = 0f;
        var t1 = 1f;

        return Clip(-dx, from.X - MinX, ref t0, ref t1)
            && Clip(dx, MaxX - from.X, ref t0, ref t1)
            && Clip(-dy, from.Y - MinY, ref t0, ref t1)
            && Clip(dy, MaxY - from.Y, ref t0, ref t1);
    }

    private static bool Clip(float p, float q, ref float t0, ref float t1)
    {
        if (p == 0f)
        {
            // The segment is parallel to this edge: it is outside when it starts beyond it.
            return q >= 0f;
        }

        var t = q / p;
        if (p < 0f)
        {
            if (t > t1)
            {
                return false;
            }

            if (t > t0)
            {
                t0 = t;
            }
        }
        else
        {
            if (t < t0)
            {
                return false;
            }

            if (t < t1)
            {
                t1 = t;
            }
        }

        return true;
    }
}
