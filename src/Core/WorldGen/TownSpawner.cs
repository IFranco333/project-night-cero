using NightCero.Core.Ecs;
using NightCero.Core.WorldModel;

namespace NightCero.Core.WorldGen;

/// <summary>
/// Turns a <see cref="TownPlan"/> into entities, so the static world is part of the simulation
/// state and therefore part of the state hash.
/// </summary>
public static class TownSpawner
{
    /// <summary>Spawns one entity per building and per prop, in plan order.</summary>
    public static void Spawn(World world, TownPlan plan)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(plan);

        foreach (var building in plan.Buildings)
        {
            world.Spawn(
                new Transform(building.Centre, 0),
                new BuildingTag(building.Kind, building.Width, building.Depth, building.Floors));
        }

        foreach (var prop in plan.Props)
        {
            world.Spawn(new Transform(prop.Position, prop.YawDegrees), new PropTag(prop.Kind));
        }
    }
}
