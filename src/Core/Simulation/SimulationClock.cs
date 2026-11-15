namespace NightCero.Core.Simulation;

/// <summary>The fixed timestep every system integrates against.</summary>
/// <remarks>
/// One source of truth for the tick length: the loop advances by <see cref="TickDuration"/> and
/// systems multiply rates by <see cref="TickSeconds"/>, so the two can never drift apart.
/// </remarks>
public static class SimulationClock
{
    /// <summary>Length of one gameplay tick.</summary>
    public static readonly TimeSpan TickDuration = TimeSpan.FromMilliseconds(100);

    /// <summary>Length of one gameplay tick in seconds, for systems that integrate over time.</summary>
    public static float TickSeconds => (float)TickDuration.TotalSeconds;
}
