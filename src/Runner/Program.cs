using System.Globalization;
using NightCero.Core.Ecs;
using NightCero.Core.Math;
using NightCero.Core.Simulation;

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

        var simulation = new Simulation(options.Seed);
        SpawnDemoBodies(simulation.World, options.Bodies, options.Seed);
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
public sealed record RunnerOptions(ulong Seed, int Ticks, int Bodies, bool ShowState)
{
    public const string Usage =
        "usage: nightcero [--seed N] [--ticks N] [--bodies N] [--state]\n" +
        "  --seed     world seed (default 1)\n" +
        "  --ticks    simulation ticks to run, 100 ms each (default 100)\n" +
        "  --bodies   demo entities to spawn (default 4)\n" +
        "  --state    print the canonical state after the run";

    /// <summary>Parses the arguments, or returns null (and the caller prints the usage).</summary>
    public static RunnerOptions? Parse(string[] args)
    {
        var seed = 1UL;
        var ticks = 100;
        var bodies = 4;
        var showState = false;

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
                case "--help" or "-h":
                    return null;
                default:
                    return null;
            }
        }

        return new RunnerOptions(seed, ticks, bodies, showState);
    }
}
