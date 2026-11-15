using NightCero.Core.Player;

namespace NightCero.Core.Simulation;

/// <summary>
/// A recorded sequence of player intents, one entry per tick. Replaying a log against the same
/// seed reproduces the session exactly (ADR 0002), which is how bugs are reported and re-tested.
/// </summary>
public sealed class InputLog
{
    private readonly List<MoveIntent?> _entries = [];

    /// <summary>Number of recorded ticks.</summary>
    public int Count => _entries.Count;

    /// <summary>The recorded intents in tick order; <c>null</c> means "no input that tick".</summary>
    public IReadOnlyList<MoveIntent?> Entries => _entries;

    /// <summary>Appends the intent submitted for the next tick.</summary>
    public void Add(MoveIntent? intent) => _entries.Add(intent);
}
