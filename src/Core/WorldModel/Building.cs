using NightCero.Core.Math;

namespace NightCero.Core.WorldModel;

/// <summary>One room inside a building, in world space at ground level.</summary>
public readonly record struct Room(int Index, Vec3 Centre, float Width, float Depth);

/// <summary>
/// A generated building: footprint, entrance and interior rooms. Produced by
/// <c>TownGenerator</c> and consumed by the navigation builder and the renderer.
/// </summary>
public readonly record struct Building(
    int Id,
    BuildingKind Kind,
    Vec3 Centre,
    float Width,
    float Depth,
    int Floors,
    Vec3 Entrance,
    IReadOnlyList<Room> Rooms)
{
    /// <summary>The footprint on the ground plane.</summary>
    public Rect2 Footprint => Rect2.FromCentre(new Vec2(Centre.X, Centre.Z), Width, Depth);
}

/// <summary>A static prop placed by the town generator.</summary>
public readonly record struct Prop(PropKind Kind, Vec3 Position, float YawDegrees);
