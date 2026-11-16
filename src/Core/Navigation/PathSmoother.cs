using NightCero.Core.Math;

namespace NightCero.Core.Navigation;

/// <summary>
/// Turns a node path into a short list of waypoints by removing nodes that a straight, clear line
/// can skip (string pulling).
/// </summary>
public static class PathSmoother
{
    /// <summary>
    /// Smooths <paramref name="path"/> using <paramref name="isClear"/> to test a straight move.
    /// The first and last waypoints are always kept.
    /// </summary>
    public static IReadOnlyList<Vec3> Smooth(
        NavGraph graph,
        IReadOnlyList<int> path,
        Func<Vec3, Vec3, bool> isClear)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(isClear);

        if (path.Count == 0)
        {
            return [];
        }

        var waypoints = new List<Vec3> { graph[path[0]].Position };
        var index = 0;

        while (index < path.Count - 1)
        {
            var next = path.Count - 1;
            for (; next > index + 1; next--)
            {
                if (isClear(graph[path[index]].Position, graph[path[next]].Position))
                {
                    break;
                }
            }

            waypoints.Add(graph[path[next]].Position);
            index = next;
        }

        return waypoints;
    }
}
