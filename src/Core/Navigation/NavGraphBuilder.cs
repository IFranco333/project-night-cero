using NightCero.Core.Math;
using NightCero.Core.WorldGen;

namespace NightCero.Core.Navigation;

/// <summary>
/// Builds a navigation graph from a generated town: road crossings, points along the roads, one
/// node per building entrance and one per interior room.
/// </summary>
/// <remarks>
/// The graph is built in a fixed order — intersections row-major, then road points, then entrances
/// and rooms in building id order — so node ids are stable for a given plan.
/// </remarks>
public static class NavGraphBuilder
{
    /// <summary>Builds the graph of <paramref name="plan"/>.</summary>
    /// <param name="plan">The town layout.</param>
    /// <param name="roadNodeSpacing">Approximate distance between road nodes, in metres.</param>
    public static NavGraph FromTown(TownPlan plan, float roadNodeSpacing = 16f)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (roadNodeSpacing <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(roadNodeSpacing), "Spacing must be positive.");
        }

        var graph = new NavGraph();
        var blocksX = plan.Width / plan.BlockPitch;
        var blocksZ = plan.Height / plan.BlockPitch;
        var intersections = new int[blocksX + 1, blocksZ + 1];

        for (var j = 0; j <= blocksZ; j++)
        {
            for (var i = 0; i <= blocksX; i++)
            {
                intersections[i, j] = graph.AddNode(
                    new Vec3(i * plan.BlockPitch, 0, j * plan.BlockPitch),
                    NavNodeKind.Intersection);
            }
        }

        for (var i = 0; i <= blocksX; i++)
        {
            for (var j = 0; j < blocksZ; j++)
            {
                ConnectAlong(
                    graph,
                    intersections[i, j],
                    intersections[i, j + 1],
                    graph[intersections[i, j]].Position,
                    vertical: true,
                    plan.BlockPitch,
                    roadNodeSpacing);
            }
        }

        for (var j = 0; j <= blocksZ; j++)
        {
            for (var i = 0; i < blocksX; i++)
            {
                ConnectAlong(
                    graph,
                    intersections[i, j],
                    intersections[i + 1, j],
                    graph[intersections[i, j]].Position,
                    vertical: false,
                    plan.BlockPitch,
                    roadNodeSpacing);
            }
        }

        var roadNodes = new List<int>();
        roadNodes.AddRange(graph.IndicesOfKind(NavNodeKind.Intersection));
        roadNodes.AddRange(graph.IndicesOfKind(NavNodeKind.Road));

        foreach (var building in plan.Buildings)
        {
            var entrance = graph.AddNode(building.Entrance, NavNodeKind.Entrance);
            var road = Nearest(roadNodes, graph, building.Entrance);
            if (road >= 0)
            {
                graph.Connect(entrance, road, Distance(graph, entrance, road));
            }

            foreach (var room in building.Rooms)
            {
                var roomNode = graph.AddNode(room.Centre, NavNodeKind.Room);
                graph.Connect(entrance, roomNode, Distance(graph, entrance, roomNode));
            }
        }

        return graph;
    }

    private static void ConnectAlong(
        NavGraph graph,
        int from,
        int to,
        Vec3 start,
        bool vertical,
        int pitch,
        float spacing)
    {
        var steps = System.Math.Max(1, (int)MathF.Round(pitch / spacing));
        var previous = from;

        for (var k = 1; k < steps; k++)
        {
            var offset = pitch * k / (float)steps;
            var position = vertical
                ? new Vec3(start.X, 0, start.Z + offset)
                : new Vec3(start.X + offset, 0, start.Z);
            var node = graph.AddNode(position, NavNodeKind.Road);
            graph.Connect(previous, node, Distance(graph, previous, node));
            previous = node;
        }

        graph.Connect(previous, to, Distance(graph, previous, to));
    }

    private static int Nearest(IReadOnlyList<int> candidates, NavGraph graph, Vec3 position)
    {
        var best = -1;
        var bestDistance = float.PositiveInfinity;

        foreach (var index in candidates)
        {
            var distance = (graph[index].Position - position).LengthXZ;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = index;
            }
        }

        return best;
    }

    private static float Distance(NavGraph graph, int from, int to) =>
        (graph[to].Position - graph[from].Position).LengthXZ;
}
