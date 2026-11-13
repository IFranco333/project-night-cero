using NightCero.Core.Ecs;
using NightCero.Core.Math;
using NightCero.Core.Simulation;

namespace NightCero.Tests.Core;

public sealed class SimulationTests
{
    private readonly record struct Body(Vec3 Position, float Speed);

    private readonly record struct Age(int Ticks);

    private sealed class DriftSystem : ISystem
    {
        public void Update(World world, TickCommands commands)
        {
            var rng = world.Rng.Fork("drift");
            var bodies = world.Query<Body>().ToArray();
            foreach (var (entity, body) in bodies)
            {
                var wobble = (float)(rng.NextDouble() - 0.5) * 0.01f;
                world.Set(entity, body with { Position = body.Position + new Vec3(body.Speed + wobble, 0, 0) });
            }

            // Consuming the stream on purpose: the sequence of draws is part of the simulation.
            _ = rng.NextULong();
        }
    }

    private sealed class LifeSystem : ISystem
    {
        public void Update(World world, TickCommands commands)
        {
            foreach (var (entity, age) in world.Query<Age>().ToArray())
            {
                world.Set(entity, new Age(age.Ticks + 1));
                if (age.Ticks >= 4)
                {
                    commands.Despawn(entity);
                    commands.Spawn(new Age(0), new Body(Vec3.Zero, 2));
                }
            }
        }
    }

    private static Simulation ThreeBodies(ulong seed)
    {
        var simulation = new Simulation(seed);
        simulation.World.Spawn(new Body(new Vec3(1, 0, 1), 1.5f), new Age(0));
        simulation.World.Spawn(new Body(new Vec3(-2, 0, 3), 0.25f), new Age(0));
        simulation.World.Spawn(new Age(3));
        simulation.World.AddSystem(new DriftSystem());
        simulation.World.AddSystem(new LifeSystem());
        return simulation;
    }

    [Fact]
    public void TheSameSeedAndTickCountProduceTheSameStateHash()
    {
        var a = ThreeBodies(2026);
        var b = ThreeBodies(2026);

        a.Run(25);
        b.Run(25);

        Assert.Equal(25, a.Tick);
        Assert.Equal(a.StateHash(), b.StateHash());
    }

    [Fact]
    public void DifferentSeedsProduceDifferentStateHashes()
    {
        var a = ThreeBodies(2026);
        var b = ThreeBodies(2027);

        a.Run(25);
        b.Run(25);

        Assert.NotEqual(a.StateHash(), b.StateHash());
    }

    [Fact]
    public void AReplayFromTheSameSeedIsHashIdentical()
    {
        var simulation = ThreeBodies(99);
        simulation.Run(10);
        var hashAtTen = simulation.StateHash();
        simulation.Run(10);

        var replay = ThreeBodies(99);
        replay.Run(10);
        var checkpoint = replay.Checkpoint();

        replay.Run(10);

        Assert.Equal(hashAtTen, checkpoint.Hash);
        Assert.Equal(simulation.StateHash(), replay.StateHash());
    }

    [Fact]
    public void TheHashChangesWhenComponentsChange()
    {
        var simulation = ThreeBodies(1);
        simulation.Run(1);
        var before = simulation.StateHash();

        var first = simulation.World.Query<Body>().First();
        simulation.World.Set(first.Entity, first.Value with { Speed = first.Value.Speed + 0.5f });

        Assert.NotEqual(before, simulation.StateHash());
    }

    [Fact]
    public void TheHashChangesWhenEntitiesAreDespawned()
    {
        var simulation = ThreeBodies(1);
        simulation.Run(1);
        var before = simulation.StateHash();

        simulation.World.Despawn(simulation.World.Query<Body>().First().Entity);

        Assert.NotEqual(before, simulation.StateHash());
    }

    [Fact]
    public void TheHashIgnoresTimeAndPresentation()
    {
        // Two simulations reach the same tick through different numbers of Run calls.
        var a = ThreeBodies(5);
        var b = ThreeBodies(5);
        a.Run(6);
        for (var i = 0; i < 3; i++)
        {
            b.Run(2);
        }

        Assert.Equal(a.StateHash(), b.StateHash());
        Assert.Equal(a.Checkpoint().State, b.Checkpoint().State);
    }

    [Fact]
    public void ACheckpointIsReproducible()
    {
        var simulation = ThreeBodies(7);
        simulation.Run(4);

        var first = simulation.Checkpoint();
        var second = simulation.Checkpoint();

        Assert.Equal(first.Tick, second.Tick);
        Assert.Equal(first.Hash, second.Hash);
        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Meta.OrderBy(p => p.Key), second.Meta.OrderBy(p => p.Key));
        Assert.Equal(simulation.Tick, first.Tick);
        Assert.Equal("7", first.Meta["seed"]);
        Assert.Equal("4", first.Meta["tick"]);
    }

    [Fact]
    public void RunRejectsNegativeTicks()
    {
        var simulation = new Simulation(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => simulation.Run(-1));
    }
}
