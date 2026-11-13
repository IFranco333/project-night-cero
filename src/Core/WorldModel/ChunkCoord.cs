using NightCero.Core.Math;

namespace NightCero.Core.WorldModel;

/// <summary>
/// The chunk that contains a world position. The world is split into 32 m squares; the index is
/// <c>floor(x / 32)</c>, <c>floor(z / 32)</c>, including for negative coordinates.
/// </summary>
public readonly record struct ChunkCoord(int X, int Z)
{
    /// <summary>Side of a chunk in metres.</summary>
    public const int Size = 32;

    /// <summary>Computes the chunk that contains a world position.</summary>
    public static ChunkCoord FromWorld(Vec3 position) =>
        new(FloorDiv(position.X, Size), FloorDiv(position.Z, Size));

    /// <summary>The centre of the chunk at ground level.</summary>
    public Vec3 Centre => new((X * Size) + (Size / 2f), 0, (Z * Size) + (Size / 2f));

    private static int FloorDiv(float value, int divisor) =>
        (int)MathF.Floor(value / divisor);

    public override string ToString() => $"Chunk({X}, {Z})";
}
