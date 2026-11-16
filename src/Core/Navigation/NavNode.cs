using NightCero.Core.Math;

namespace NightCero.Core.Navigation;

/// <summary>What a navigation node represents.</summary>
public enum NavNodeKind
{
    /// <summary>A road crossing.</summary>
    Intersection,

    /// <summary>A point along a road between two crossings.</summary>
    Road,

    /// <summary>A building door.</summary>
    Entrance,

    /// <summary>The centre of a room inside a building.</summary>
    Room,
}

/// <summary>A node of the navigation graph, at ground level.</summary>
public readonly record struct NavNode(int Id, Vec3 Position, NavNodeKind Kind);

/// <summary>A directed edge of the navigation graph. Every edge is stored in both directions.</summary>
public readonly record struct NavEdge(int To, float Cost);
