using NightCero.Core.Math;

namespace NightCero.Core.Navigation;

/// <summary>
/// A* over a <see cref="NavGraph"/>, with Euclidean distance as the admissible heuristic.
/// </summary>
/// <remarks>
/// Ties are broken by node id (the priority is <c>(estimate, id)</c>), so the returned path is the
/// same on every run and on every platform — a replay must reproduce not only the destination but
/// the route (ADR 0002).
/// </remarks>
public static class AStar
{
    /// <summary>Finds the cheapest path from <paramref name="start"/> to <paramref name="goal"/>.</summary>
    /// <returns>The node ids including both ends, or null when no path exists.</returns>
    public static IReadOnlyList<int>? FindPath(NavGraph graph, int start, int goal)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(start, graph.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(goal);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(goal, graph.Count);

        if (start == goal)
        {
            return [start];
        }

        var count = graph.Count;
        var cost = new float[count];
        Array.Fill(cost, float.PositiveInfinity);
        var cameFrom = new int[count];
        Array.Fill(cameFrom, -1);
        var closed = new bool[count];

        var open = new PriorityQueue<int, (float Estimate, int Id)>();
        cost[start] = 0f;
        open.Enqueue(start, (Heuristic(graph, start, goal), start));

        while (open.TryDequeue(out var current, out _))
        {
            if (closed[current])
            {
                continue;
            }

            if (current == goal)
            {
                return Reconstruct(cameFrom, start, goal);
            }

            closed[current] = true;

            foreach (var edge in graph.Neighbours(current))
            {
                if (closed[edge.To])
                {
                    continue;
                }

                var tentative = cost[current] + edge.Cost;
                if (tentative >= cost[edge.To])
                {
                    continue;
                }

                cost[edge.To] = tentative;
                cameFrom[edge.To] = current;
                open.Enqueue(edge.To, (tentative + Heuristic(graph, edge.To, goal), edge.To));
            }
        }

        return null;
    }

    /// <summary>The cost of a path returned by <see cref="FindPath"/>.</summary>
    public static float PathCost(NavGraph graph, IReadOnlyList<int> path)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(path);

        var total = 0f;
        for (var i = 1; i < path.Count; i++)
        {
            total += (graph[path[i]].Position - graph[path[i - 1]].Position).LengthXZ;
        }

        return total;
    }

    private static float Heuristic(NavGraph graph, int from, int to) =>
        (graph[to].Position - graph[from].Position).LengthXZ;

    private static IReadOnlyList<int> Reconstruct(int[] cameFrom, int start, int goal)
    {
        var path = new List<int>();
        for (var node = goal; node != -1; node = cameFrom[node])
        {
            path.Add(node);
            if (node == start)
            {
                break;
            }
        }

        path.Reverse();
        return path;
    }
}
