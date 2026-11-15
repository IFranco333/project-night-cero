using NightCero.Core.Math;
using NightCero.Core.Player;
using NightCero.Core.Simulation;
using NightCero.Core.WorldModel;

namespace NightCero.Tests.Support;

/// <summary>A minimal player scene: one player entity driven by <see cref="PlayerMovementSystem"/>.</summary>
public static class PlayerScene
{
    public static Simulation Create(ulong seed = 1)
    {
        var simulation = new Simulation(seed);
        var player = simulation.World.Spawn(
            new Transform(Vec3.Zero, 0),
            new PlayerMoveState(Vec2.Zero, Sprinting: false));
        simulation.BindPlayer(player);
        simulation.World.AddSystem(new PlayerMovementSystem());
        return simulation;
    }
}
