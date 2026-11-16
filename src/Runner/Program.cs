using System.Diagnostics;
using System.Globalization;
using NightCero.Content;
using NightCero.Core.Ecs;
using NightCero.Core.Math;
using NightCero.Core.Navigation;
using NightCero.Core.Player;
using NightCero.Core.Simulation;
using NightCero.Core.WorldGen;
using NightCero.Core.WorldModel;

namespace NightCero.Runner;

/// <summary>
/// Headless entry point: runs the simulation without Godot for replays, balance batches and CI.
/// </summary>
/// <example>
/// <code>
/// dotnet run --project src/Runner -- --seed 2026 --ticks 250 --bodies 8
/// </code>
/// </example>
public static class Program
{
    public static int Main(string[] args)
    {
        var options = RunnerOptions.Parse(args);
        if (options is null)
        {
            Console.Error.WriteLine(RunnerOptions.Usage);
            return 2;
        }

        if (options.Bench)
        {
            return RunBenchmark(options);
        }

        var simulation = new Simulation(options.Seed);
        if (options.Town || options.Nav)
        {
            var plan = TownGenerator.Generate(options.Seed, TownSettings.Default, BuildingCatalog.All);
            TownSpawner.Spawn(simulation.World, plan);
            Console.WriteLine($"town     {plan.Buildings.Count} buildings, {plan.Props.Count} props, {plan.Roads.Count} roads");
            Console.WriteLine($"townhash {plan.Hash():x16}");

            if (options.Nav)
            {
                PrintNavigation(plan);
            }
        }
        else
        {
            SpawnDemoBodies(simulation.World, options.Bodies, options.Seed);
        }

        simulation.Run(options.Ticks);

        var checkpoint = simulation.Checkpoint();
        Console.WriteLine($"seed     {simulation.Seed}");
        Console.WriteLine($"ticks    {simulation.Tick}");
        Console.WriteLine($"entities {simulation.World.HighestEntityId}");
        Console.WriteLine($"hash     {checkpoint.Hash:x16}");
        if (options.ShowState)
        {
            Console.WriteLine(checkpoint.State);
        }

        return 0;
    }

    /// <summary>
    /// Measures steady-state tick throughput. Timing lives here, in the runner, never in the
    /// deterministic core; the printed hash still proves the run is reproducible.
    /// </summary>
    private static int RunBenchmark(RunnerOptions options)
    {
        var simulation = new Simulation(options.Seed);
        SpawnMovingBodies(simulation.World, options.Bodies);
        simulation.World.AddSystem(new PlayerMovementSystem());

        // Warm up the JIT so the measurement reflects steady-state work.
        simulation.Run(Math.Min(10, options.Ticks));

        var stopwatch = Stopwatch.StartNew();
        simulation.Run(options.Ticks);
        stopwatch.Stop();

        var seconds = stopwatch.Elapsed.TotalSeconds;
        var ticksPerSecond = seconds > 0 ? options.Ticks / seconds : double.PositiveInfinity;
        var millisecondsPerTick = options.Ticks > 0 ? stopwatch.Elapsed.TotalMilliseconds / options.Ticks : 0;

        Console.WriteLine($"seed     {simulation.Seed}");
        Console.WriteLine($"ticks    {options.Ticks}");
        Console.WriteLine($"entities {simulation.World.HighestEntityId}");
        Console.WriteLine($"elapsed  {stopwatch.Elapsed.TotalMilliseconds:F1} ms");
        Console.WriteLine($"ticks/s  {ticksPerSecond:F0}");
        Console.WriteLine($"ms/tick  {millisecondsPerTick:F4}");
        Console.WriteLine($"hash     {simulation.Checkpoint().Hash:x16}");
        return 0;
    }

    /// <summary>Builds the navigation graph and reports a route between two building entrances.</summary>
    private static void PrintNavigation(TownPlan plan)
    {
        var graph = NavGraphBuilder.FromTown(plan);
        var roadNodes = graph.IndicesOfKind(NavNodeKind.Intersection).Count
            + graph.IndicesOfKind(NavNodeKind.Road).Count;
        Console.WriteLine($"nav      {graph.Count} nodes, {roadNodes} on roads");

        var entrances = graph.IndicesOfKind(NavNodeKind.Entrance);
        var path = AStar.FindPath(graph, entrances[0], entrances[^1]);
        if (path is null)
        {
            Console.WriteLine("path     none");
            return;
        }

        var waypoints = PathSmoother.Smooth(
            graph,
            path,
            (from, to) => TownGeometry.IsSegmentClear(plan, from, to, radius: 0.4f));
        Console.WriteLine(
            $"path     {path.Count} nodes, {waypoints.Count} waypoints, {AStar.PathCost(graph, path):F1} m");
    }

    /// <summary>Bodies with a transform and a movement intent, so the benchmark exercises the ECS.</summary>
    private static void SpawnMovingBodies(World world, int count)
    {
        var rng = world.Rng.Fork("runner-bench");
        for (var i = 0; i < count; i++)
        {
            var position = new Vec3(rng.NextInt(-16, 16), 0, rng.NextInt(-16, 16));
            var direction = new Vec2(rng.NextInt(-10, 10) / 10f, rng.NextInt(-10, 10) / 10f);
            world.Spawn(new Transform(position, 0), new PlayerMoveState(direction, Sprinting: false));
        }
    }

    /// <summary>A small deterministic scene: bodies drifting east with per-entity speeds.</summary>
    private static void SpawnDemoBodies(World world, int count, ulong seed)
    {
        var rng = world.Rng.Fork("runner-demo");
        for (var i = 0; i < count; i++)
        {
            var position = new Vec3(rng.NextInt(-16, 16), 0, rng.NextInt(-16, 16));
            var speed = rng.NextInt(1, 30) / 10f;
            world.Spawn(new Body(position, speed));
        }

        _ = seed;
    }

    private readonly record struct Body(Vec3 Position, float Speed);
}

/// <summary>Parsed command-line options of the runner.</summary>
public sealed record RunnerOptions(
    ulong Seed,
    int Ticks,
    int Bodies,
    bool ShowState,
    bool Bench = false,
    bool Town = false,
    bool Nav = false)
{
    public const string Usage =
        "usage: nightcero [--seed N] [--ticks N] [--bodies N] [--state] [--bench] [--town] [--nav]\n" +
        "  --seed     world seed (default 1)\n" +
        "  --ticks    simulation ticks to run, 100 ms each (default 100)\n" +
        "  --bodies   demo entities to spawn (default 4)\n" +
        "  --state    print the canonical state after the run\n" +
        "  --bench    measure tick throughput instead of printing the state\n" +
        "  --town     generate the town for the seed instead of demo bodies\n" +
        "  --nav      also build the navigation graph and report a route (implies --town)";

    /// <summary>Parses the arguments, or returns null (and the caller prints the usage).</summary>
    public static RunnerOptions? Parse(string[] args)
    {
        var seed = 1UL;
        var ticks = 100;
        var bodies = 4;
        var showState = false;
        var bench = false;
        var town = false;
        var nav = false;

        for (var i = 0; i < args.Length; i++)
        {
            var value = () =>
            {
                if (i + 1 >= args.Length)
                {
                    return (string?)null;
                }

                return args[++i];
            };

            switch (args[i])
            {
                case "--seed" when value() is { } s && ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSeed):
                    seed = parsedSeed;
                    break;
                case "--ticks" when value() is { } t && int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedTicks) && parsedTicks >= 0:
                    ticks = parsedTicks;
                    break;
                case "--bodies" when value() is { } b && int.TryParse(b, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedBodies) && parsedBodies >= 0:
                    bodies = parsedBodies;
                    break;
                case "--state":
                    showState = true;
                    break;
                case "--bench":
                    bench = true;
                    break;
                case "--town":
                    town = true;
                    break;
                case "--nav":
                    nav = true;
                    break;
                case "--help" or "-h":
                    return null;
                default:
                    return null;
            }
        }

        return new RunnerOptions(seed, ticks, bodies, showState, bench, town, nav);
    }
}
