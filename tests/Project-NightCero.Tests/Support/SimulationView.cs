using NightCero.Core.Player;
using NightCero.Core.Simulation;
using NightCero.Core.WorldModel;

namespace NightCero.Tests.Support;

/// <summary>Test-only readers that stand in for the presentation layer.</summary>
public static class SimulationView
{
    /// <summary>The player's current pose and intent.</summary>
    public static (Transform Transform, PlayerMoveState Movement) PlayerView(this Simulation simulation) =>
        (simulation.World.Get<Transform>(simulation.Player), simulation.World.Get<PlayerMoveState>(simulation.Player));
}
