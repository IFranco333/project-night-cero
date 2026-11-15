using System.Globalization;
using NightCero.Core.Ecs;
using NightCero.Core.Player;

namespace NightCero.Core.Simulation;

/// <summary>An immutable observation of the simulation at one tick.</summary>
/// <param name="Tick">The tick the checkpoint was taken after.</param>
/// <param name="Hash">The <see cref="StateHasher"/> hash at that tick.</param>
/// <param name="Meta">Seed, tick and state size, suitable for a save header.</param>
/// <param name="State">The canonical state text.</param>
public sealed record SimulationCheckpoint(
    long Tick,
    ulong Hash,
    IReadOnlyDictionary<string, string> Meta,
    string State);

/// <summary>
/// A deterministic simulation run: one world, its seed and a fixed tick loop.
/// </summary>
/// <remarks>
/// The same seed and the same number of ticks always produce the same state and the same
/// <see cref="StateHash"/>; nothing in the loop reads the wall clock or unseeded randomness
/// (ADR 0002). The Godot layer drives this class and never reaches into the world to mutate it.
/// </remarks>
public sealed class Simulation
{
    /// <summary>Length of one gameplay tick.</summary>
    public static TimeSpan TickDuration => SimulationClock.TickDuration;

    public Simulation(ulong seed) => World = new World(seed);

    /// <summary>The low-level entity–component world. Systems live here.</summary>
    public World World { get; }

    /// <summary>The entity the presentation layer follows and controls, if any.</summary>
    public Entity Player { get; private set; }

    /// <summary>
    /// Declares the player entity. Presentation follows it; <see cref="Submit(MoveIntent)"/>
    /// targets it.
    /// </summary>
    public void BindPlayer(Entity player) => Player = player;

    /// <summary>Applies a movement intent to the player for the next tick.</summary>
    public void Submit(MoveIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        if (Player.IsNone)
        {
            throw new InvalidOperationException("No player is bound; call BindPlayer first.");
        }

        World.Set(Player, new PlayerMoveState(intent.Direction, intent.Sprint));
        LastIntent = intent;
    }

    /// <summary>The last movement intent submitted, for replay logs and the headless runner.</summary>
    public MoveIntent? LastIntent { get; private set; }

    /// <summary>The world seed.</summary>
    public ulong Seed => World.Seed;

    /// <summary>Ticks simulated so far.</summary>
    public long Tick => World.TickCount;

    /// <summary>Runs <paramref name="ticks"/> simulation ticks.</summary>
    public void Run(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        for (var i = 0; i < ticks; i++)
        {
            World.Tick();
        }
    }

    /// <summary>
    /// Replays a recorded input log, one tick per entry, submitting each intent before its tick.
    /// The same seed and log always reach the same state (ADR 0002).
    /// </summary>
    public void Run(InputLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        foreach (var intent in log.Entries)
        {
            if (intent is not null)
            {
                Submit(intent);
            }

            World.Tick();
        }
    }

    /// <summary>Hash of the current state.</summary>
    public ulong StateHash() => StateHasher.Hash(World);

    /// <summary>Takes a reproducible checkpoint of the current state.</summary>
    public SimulationCheckpoint Checkpoint()
    {
        var state = StateHasher.Canonical(World);
        var meta = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["seed"] = Seed.ToString(CultureInfo.InvariantCulture),
            ["tick"] = Tick.ToString(CultureInfo.InvariantCulture),
            ["bytes"] = state.Length.ToString(CultureInfo.InvariantCulture),
        };

        return new SimulationCheckpoint(Tick, StateHasher.Hash(World), meta, state);
    }
}
