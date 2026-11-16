using NightCero.Core.Math;

namespace NightCero.Core.WorldGen;

/// <summary>Walkability queries against the static town geometry.</summary>
public static class TownGeometry
{
    /// <summary>
    /// Whether a body of <paramref name="radius"/> can stand at <paramref name="position"/>: inside
    /// the town and clear of every building footprint.
    /// </summary>
    public static bool IsWalkable(TownPlan plan, Vec3 position, float radius = 0f)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var point = new Vec2(position.X, position.Z);
        if (!plan.Bounds.Expand(-radius).Contains(point))
        {
            return false;
        }

        foreach (var building in plan.Buildings)
        {
            if (building.Footprint.Expand(radius).Contains(point))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Whether a straight move from <paramref name="from"/> to <paramref name="to"/> avoids every
    /// building footprint. Used to smooth navigation paths into straight runs.
    /// </summary>
    public static bool IsSegmentClear(TownPlan plan, Vec3 from, Vec3 to, float radius = 0f)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var a = new Vec2(from.X, from.Z);
        var b = new Vec2(to.X, to.Z);

        foreach (var building in plan.Buildings)
        {
            if (building.Footprint.Expand(radius).IntersectsSegment(a, b))
            {
                return false;
            }
        }

        return true;
    }
}
