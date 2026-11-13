namespace NightCero.Core.Ecs;

/// <summary>
/// A system updates a subset of the world every tick, in registration order.
/// </summary>
/// <remarks>
/// Systems may read and write component values, and request structural changes (spawn, despawn,
/// add/remove components) through <see cref="TickCommands"/>; those requests are applied together
/// after every system has run, which keeps iteration stable during a tick.
/// </remarks>
public interface ISystem
{
    void Update(World world, TickCommands commands);
}
