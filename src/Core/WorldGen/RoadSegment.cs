using NightCero.Core.Math;

namespace NightCero.Core.WorldGen;

/// <summary>An axis-aligned road centreline at ground level.</summary>
public readonly record struct RoadSegment(Vec3 Start, Vec3 End, float Width)
{
    /// <summary>Whether the segment runs east–west.</summary>
    public bool IsHorizontal => Start.Z == End.Z;

    /// <summary>Length of the centreline in metres.</summary>
    public float Length => (End - Start).LengthXZ;
}
