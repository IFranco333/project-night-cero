using NightCero.Core.Math;
using NightCero.Core.Navigation;
using NightCero.Core.WorldGen;
using NightCero.Tests.Support;

namespace NightCero.Tests.Navigation;

public sealed class NavGraphTests
{
    [Fact]
    public void EveryEntranceIsConnectedToTheRoadNetwork()
    {
        var graph = NavGraphBuilder.FromTown(TownFixtures.Plan());
        var reachable = Reachable(graph, graph.IndicesOfKind(NavNodeKind.Intersection)[0]);

        var entrances = graph.IndicesOfKind(NavNodeKind.Entrance);
        Assert.NotEmpty(entrances);
        Assert.All(entrances, entrance => Assert.Contains(entrance, reachable));
    }

    [Fact]
    public void EveryRoomIsConnectedToTheRoadNetwork()
    {
        var graph = NavGraphBuilder.FromTown(TownFixtures.Plan());
        var reachable = Reachable(graph, graph.IndicesOfKind(NavNodeKind.Intersection)[0]);

        var rooms = graph.IndicesOfKind(NavNodeKind.Room);
        Assert.NotEmpty(rooms);
        Assert.All(rooms, room => Assert.Contains(room, reachable));
    }

    [Fact]
    public void APathExistsBetweenDistantEntrances()
    {
        var graph = NavGraphBuilder.FromTown(TownFixtures.Plan());
        var entrances = graph.IndicesOfKind(NavNodeKind.Entrance);

        var path = AStar.FindPath(graph, entrances[0], entrances[^1]);

        Assert.NotNull(path);
        Assert.Equal(entrances[0], path[0]);
        Assert.Equal(entrances[^1], path[^1]);
        Assert.True(AStar.PathCost(graph, path) > 0);
    }

    [Fact]
    public void TheGraphIsDeterministicForASeed()
    {
        var a = NavGraphBuilder.FromTown(TownFixtures.Plan(2026));
        var b = NavGraphBuilder.FromTown(TownFixtures.Plan(2026));

        Assert.Equal(a.Count, b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i], b[i]);
            Assert.Equal(a.Neighbours(i), b.Neighbours(i));
        }
    }

    [Fact]
    public void AStarFindsTheCheapestRouteOnAHandBuiltGraph()
    {
        var graph = new NavGraph();
        var a = graph.AddNode(new Vec3(0, 0, 0), NavNodeKind.Road);
        var b = graph.AddNode(new Vec3(10, 0, 0), NavNodeKind.Road);
        var c = graph.AddNode(new Vec3(20, 0, 0), NavNodeKind.Road);
        var d = graph.AddNode(new Vec3(10, 0, 10), NavNodeKind.Road);
        graph.Connect(a, b, 10);
        graph.Connect(b, c, 10);
        graph.Connect(a, d, 10);
        graph.Connect(d, c, 10);

        var path = AStar.FindPath(graph, a, c);

        Assert.NotNull(path);
        Assert.Equal(new[] { a, b, c }, path);
        Assert.Equal(20f, AStar.PathCost(graph, path), 3);
    }

    [Fact]
    public void AStarReturnsNullWhenTheGoalIsUnreachable()
    {
        var graph = new NavGraph();
        var a = graph.AddNode(Vec3.Zero, NavNodeKind.Road);
        graph.AddNode(new Vec3(10, 0, 0), NavNodeKind.Road);

        Assert.Null(AStar.FindPath(graph, a, 1));
    }

    [Fact]
    public void AStarReturnsTheNodeItselfWhenStartEqualsGoal()
    {
        var graph = new NavGraph();
        var a = graph.AddNode(Vec3.Zero, NavNodeKind.Road);

        Assert.Equal(new[] { a }, AStar.FindPath(graph, a, a));
    }

    [Fact]
    public void SmoothingKeepsTheEndpointsAndDropsRedundantWaypoints()
    {
        var graph = new NavGraph();
        var a = graph.AddNode(new Vec3(0, 0, 0), NavNodeKind.Road);
        var b = graph.AddNode(new Vec3(10, 0, 0), NavNodeKind.Road);
        var c = graph.AddNode(new Vec3(20, 0, 0), NavNodeKind.Road);

        var waypoints = PathSmoother.Smooth(graph, [a, b, c], (_, _) => true);

        Assert.Equal(2, waypoints.Count);
        Assert.Equal(new Vec3(0, 0, 0), waypoints[0]);
        Assert.Equal(new Vec3(20, 0, 0), waypoints[1]);
    }

    [Fact]
    public void SmoothingKeepsAnIntermediateNodeWhenTheShortcutIsBlocked()
    {
        var graph = new NavGraph();
        var a = graph.AddNode(new Vec3(0, 0, 0), NavNodeKind.Road);
        var b = graph.AddNode(new Vec3(10, 0, 0), NavNodeKind.Road);
        var c = graph.AddNode(new Vec3(20, 0, 0), NavNodeKind.Road);

        var waypoints = PathSmoother.Smooth(
            graph,
            [a, b, c],
            (from, to) => !(from == new Vec3(0, 0, 0) && to == new Vec3(20, 0, 0)));

        Assert.Equal(3, waypoints.Count);
        Assert.Equal(new Vec3(10, 0, 0), waypoints[1]);
    }

    [Fact]
    public void NearestNodeReturnsTheClosestNode()
    {
        var graph = new NavGraph();
        var a = graph.AddNode(new Vec3(0, 0, 0), NavNodeKind.Road);
        var b = graph.AddNode(new Vec3(100, 0, 0), NavNodeKind.Road);

        Assert.Equal(a, graph.NearestNode(new Vec3(10, 0, 0)));
        Assert.Equal(b, graph.NearestNode(new Vec3(90, 0, 0)));
    }

    [Fact]
    public void AnEmptyGraphHasNoNearestNode()
    {
        Assert.Throws<InvalidOperationException>(() => new NavGraph().NearestNode(Vec3.Zero));
    }

    private static HashSet<int> Reachable(NavGraph graph, int start)
    {
        var seen = new HashSet<int> { start };
        var queue = new Queue<int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            foreach (var edge in graph.Neighbours(queue.Dequeue()))
            {
                if (seen.Add(edge.To))
                {
                    queue.Enqueue(edge.To);
                }
            }
        }

        return seen;
    }
}
