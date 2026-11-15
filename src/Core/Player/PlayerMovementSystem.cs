using NightCero.Core.Ecs;
using NightCero.Core.Math;
using NightCero.Core.Simulation;
using NightCero.Core.WorldModel;

namespace NightCero.Core.Player;

/// <summary>
/// Moves every entity that has a <see cref="Transform"/> and a <see cref="PlayerMoveState"/> by
/// its current intent. Runs on the fixed tick, so movement is independent of frame rate.
/// </summary>
public sealed class PlayerMovementSystem : ISystem
{
    public void Update(World world, TickCommands commands)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(commands);

        foreach (var (entity, state) in world.Query<PlayerMoveState>().ToArray())
        {
            if (!world.Has<Transform>(entity))
            {
                continue;
            }

            var direction = state.MoveDirection.Normalized();
            if (direction == Vec2.Zero)
            {
                continue;
            }

            var transform = world.Get<Transform>(entity);
            var speed = (state.Sprinting ? MoveIntent.SprintSpeedMetersPerSecond : MoveIntent.WalkSpeedMetersPerSecond)
                * SimulationClock.TickSeconds;
            var yaw = MathF.Atan2(direction.X, -direction.Y) * (180f / MathF.PI);
            var moved = transform with
            {
                Position = transform.Position + (direction * speed).ToXZ(),
                YawDegrees = yaw,
            };

            // Defer the write so other systems in this tick see a consistent snapshot (ADR 0002).
            commands.Set(entity, moved);
        }
    }
}
