using NightCero.Core.Math;

namespace NightCero.Core.Navigation;

/// <summary>
/// A weighted, undirected graph of walkable points. Nodes are numbered in insertion order, which is
/// deterministic, and neighbours are stored in insertion order, so path finding is reproducible.
/// </summary>
public sealed class NavGraph
{
    private readonly List<NavNode> _nodes = [];
    private readonly List<List<NavEdge>> _edges = [];

    /// <summary>All nodes, in id order.</summary>
    public IReadOnlyList<NavNode> Nodes => _nodes;

    /// <summary>Number of nodes.</summary>
    public int Count => _nodes.Count;

    /// <summary>The node with the given id.</summary>
    public NavNode this[int index] => _nodes[index];

    /// <summary>Adds a node and returns its id.</summary>
    internal int AddNode(Vec3 position, NavNodeKind kind)
    {
        var id = _nodes.Count;
        _nodes.Add(new NavNode(id, position, kind));
        _edges.Add([]);
        return id;
    }

    /// <summary>Connects two nodes in both directions with the same cost.</summary>
    internal void Connect(int from, int to, float cost)
    {
        if (from == to)
        {
            return;
        }

        _edges[from].Add(new NavEdge(to, cost));
        _edges[to].Add(new NavEdge(from, cost));
    }

    /// <summary>The edges leaving a node, in insertion order.</summary>
    public IReadOnlyList<NavEdge> Neighbours(int node) => _edges[node];

    /// <summary>The ids of every node of a kind, in id order.</summary>
    public IReadOnlyList<int> IndicesOfKind(NavNodeKind kind)
    {
        var indices = new List<int>();
        for (var i = 0; i < _nodes.Count; i++)
        {
            if (_nodes[i].Kind == kind)
            {
                indices.Add(i);
            }
        }

        return indices;
    }

    /// <summary>The node closest to a world position; ties go to the lowest id.</summary>
    public int NearestNode(Vec3 position)
    {
        if (_nodes.Count == 0)
        {
            throw new InvalidOperationException("The graph has no nodes.");
        }

        var best = 0;
        var bestDistance = float.PositiveInfinity;

        for (var i = 0; i < _nodes.Count; i++)
        {
            var distance = (_nodes[i].Position - position).LengthXZ;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }
}
