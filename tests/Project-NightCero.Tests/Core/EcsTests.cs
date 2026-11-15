using NightCero.Core.Ecs;

namespace NightCero.Tests.Core;

public sealed class EcsTests
{
    private readonly record struct Position(float X, float Y);

    private readonly record struct Health(int Value);

    private sealed class MoveSystem : ISystem
    {
        public int Calls;

        public void Update(World world, TickCommands commands)
        {
            Calls++;
            var positions = world.Query<Position>().ToArray();
            foreach (var (entity, position) in positions)
            {
                world.Set(entity, position with { X = position.X + 1 });
            }
        }
    }

    [Fact]
    public void EntitiesGetMonotonicIdsStartingAtOne()
    {
        var world = new World(1);

        var first = world.Spawn();
        var second = world.Spawn();
        var third = world.Spawn();

        Assert.Equal(1u, first.Value);
        Assert.Equal(2u, second.Value);
        Assert.Equal(3u, third.Value);
    }

    [Fact]
    public void ComponentsRoundTrip()
    {
        var world = new World(1);
        var entity = world.Spawn();

        world.Set(entity, new Position(3, 4));

        Assert.True(world.Has<Position>(entity));
        Assert.Equal(new Position(3, 4), world.Get<Position>(entity));
    }

    [Fact]
    public void SpawnWithComponentsReturnsAReadyEntity()
    {
        var world = new World(1);

        var entity = world.Spawn(new Position(1, 2), new Health(10));

        Assert.Equal(new Position(1, 2), world.Get<Position>(entity));
        Assert.Equal(10, world.Get<Health>(entity).Value);
    }

    [Fact]
    public void QueryReturnsOnlyLivingEntitiesWithTheComponent()
    {
        var world = new World(1);
        var a = world.Spawn();
        var b = world.Spawn();
        var c = world.Spawn();
        world.Set(a, new Position(1, 0));
        world.Set(c, new Position(3, 0));
        world.Set(c, new Health(5));

        var found = world.Query<Position>().Select(pair => pair.Entity).ToArray();

        Assert.Equal(new[] { a, c }, found);
        _ = b;
    }

    [Fact]
    public void QueryIteratesInEntityIdOrder()
    {
        var world = new World(1);
        var ids = Enumerable.Range(0, 64).Select(_ => world.Spawn()).ToArray();

        // Walk ids backwards on purpose: insertion order must not affect iteration order. An
        // explicit loop avoids the array.Reverse() overload ambiguity (LINQ vs. Span in-place).
        for (var i = ids.Length - 1; i >= 0; i--)
        {
            world.Set(ids[i], new Position(ids[i].Value, 0));
        }
        world.Despawn(ids[10]);

        var found = world.Query<Position>().Select(pair => pair.Entity.Value).ToArray();

        Assert.Equal(ids.Where(id => id != ids[10]).Select(id => id.Value).OrderBy(v => v), found);
    }

    [Fact]
    public void DespawnRemovesTheEntityAndItsComponents()
    {
        var world = new World(1);
        var entity = world.Spawn(new Position(1, 1));

        world.Despawn(entity);

        Assert.False(world.IsAlive(entity));
        Assert.False(world.Has<Position>(entity));
        Assert.Empty(world.Query<Position>());
    }

    [Fact]
    public void OperationsOnDespawnedEntitiesThrow()
    {
        var world = new World(1);
        var entity = world.Spawn(new Position(1, 1));
        world.Despawn(entity);

        Assert.Throws<InvalidOperationException>(() => world.Set(entity, new Health(1)));
        Assert.Throws<InvalidOperationException>(() => world.Get<Position>(entity));
    }

    [Fact]
    public void StructuralChangesAreDeferredToTheEndOfTheTick()
    {
        var world = new World(1);
        var healthDuringSystems = -1;
        world.AddSystem(new SpawnOnTickSystem());
        world.AddSystem(new LambdaSystem((w, _) => healthDuringSystems = w.Query<Health>().Count()));

        world.Tick();

        Assert.Equal(0, healthDuringSystems);
        Assert.Single(world.Query<Health>());
    }

    [Fact]
    public void DeferredSpawnsAreAliveOnlyAfterTheTick()
    {
        var world = new World(1);
        var first = world.Spawn();
        Entity spawned = default;
        var aliveDuringTick = false;
        world.AddSystem(new LambdaSystem((w, c) =>
        {
            spawned = c.Spawn(new Health(3));
            aliveDuringTick = w.IsAlive(spawned);
        }));

        world.Tick();

        Assert.False(aliveDuringTick);
        Assert.True(world.IsAlive(spawned));
        Assert.True(spawned.Value > first.Value);
        Assert.True(world.Has<Health>(spawned));
    }

    [Fact]
    public void SystemsRunInRegistrationOrder()
    {
        var world = new World(1);
        var log = new List<string>();
        world.AddSystem(new LambdaSystem((_, _) => log.Add("a")));
        world.AddSystem(new LambdaSystem((_, _) => log.Add("b")));
        world.AddSystem(new LambdaSystem((_, _) => log.Add("c")));

        world.Tick();

        Assert.Equal(new[] { "a", "b", "c" }, log);
    }

    [Fact]
    public void TickIncrementsTheTickCounter()
    {
        var world = new World(1);
        Assert.Equal(0L, world.TickCount);

        world.Tick();
        world.Tick();

        Assert.Equal(2L, world.TickCount);
    }

    [Fact]
    public void EventsAreDeliveredToSubscribersAndClearedAtEndOfTick()
    {
        var world = new World(1);
        var received = new List<int>();
        int? readAfterClear = null;
        world.Subscribe<int>(value => received.Add(value));
        world.AddSystem(new LambdaSystem((w, _) =>
        {
            w.Publish(1);
            w.Publish(2);
            readAfterClear = w.Published<int>().Count();
        }));

        world.Tick();

        Assert.Equal(new[] { 1, 2 }, received);
        Assert.Equal(2, readAfterClear);
        Assert.Empty(world.Published<int>());
    }

    [Fact]
    public void EventsPublishedDuringATickReachSubscribersOnTheNextTick()
    {
        var world = new World(1);
        var received = new List<int>();
        world.Subscribe<int>(received.Add);
        world.AddSystem(new LambdaSystem((w, _) => w.Publish(42)));

        // Events published while the systems run are delivered at the end of that same tick.
        world.Tick();
        Assert.Equal(new[] { 42 }, received);
    }

    [Fact]
    public void UnsubscribingStopsDelivery()
    {
        var world = new World(1);
        var count = 0;
        Action<int> handler = _ => count++;
        world.Subscribe(handler);
        world.Publish(1);
        world.Tick();

        world.Unsubscribe(handler);
        world.Publish(2);
        world.Tick();

        Assert.Equal(1, count);
    }

    [Fact]
    public void SystemsMutateTheWorldWalkthrough()
    {
        var world = new World(1);
        var entity = world.Spawn(new Position(0, 0));
        var move = new MoveSystem();
        world.AddSystem(move);

        world.Tick();

        Assert.Equal(1, move.Calls);
        Assert.Equal(1f, world.Get<Position>(entity).X);
        Assert.Equal(0f, world.Get<Position>(entity).Y);
    }

    // ---- Regression tests for the review findings -------------------------------------------

    [Fact]
    public void WorldsDoNotShareComponentStorage()
    {
        var a = new World(1);
        var b = new World(1);
        var inA = a.Spawn(new Position(1, 2));
        var inB = b.Spawn();

        Assert.True(a.Has<Position>(inA));
        Assert.False(b.Has<Position>(inB));
        Assert.Empty(b.Query<Position>());
    }

    [Fact]
    public void SetThenDespawnLeavesNoRowsBehind()
    {
        var world = new World(1);
        var entity = world.Spawn();
        world.Set(entity, new Position(1, 1));

        world.Despawn(entity);

        Assert.False(world.Has<Position>(entity));
        Assert.Empty(world.Query<Position>());
    }

    [Fact]
    public void DirectStructuralChangesDuringATickAreRejected()
    {
        var world = new World(1);
        var error = (Exception?)null;
        world.AddSystem(new LambdaSystem((w, _) =>
        {
            try
            {
                w.Spawn();
            }
            catch (InvalidOperationException ex)
            {
                error = ex;
            }
        }));

        world.Tick();

        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void ADespawnFollowedByChangesInTheSameTickDoesNotAbortTheTick()
    {
        var world = new World(1);
        world.AddSystem(new LambdaSystem((_, c) =>
        {
            var doomed = c.Spawn(new Health(1));
            c.Despawn(doomed);
            c.Set(doomed, new Health(2));
            c.Remove<Health>(doomed);
        }));

        world.Tick();

        Assert.Equal(1L, world.TickCount);
        Assert.Empty(world.Query<Health>());
    }

    [Fact]
    public void UnsubscribeRemovesOnlyTheGivenHandler()
    {
        var world = new World(1);
        var received = new List<string>();
        void First(string _) => received.Add("first");
        void Second(string _) => received.Add("second");
        Action<string> staticHandler = _ => received.Add("static");
        world.Subscribe<string>(First);
        world.Subscribe<string>(Second);
        world.Subscribe(staticHandler);

        world.Unsubscribe<string>(First);

        world.Publish("x");
        world.Tick();

        Assert.Equal(new[] { "second", "static" }, received);
    }

    [Fact]
    public void SubscribersToABaseTypeReceiveDerivedEvents()
    {
        var world = new World(1);
        var received = new List<string>();
        world.Subscribe<object>(value => received.Add(value.GetType().Name));
        world.Subscribe<Health>(_ => received.Add("dot"));

        world.Publish(new Health(1));
        world.Tick();

        Assert.Equal(new[] { nameof(Health), "dot" }, received);
    }

    [Fact]
    public void ReferenceTypeComponentsAreRejected()
    {
        var world = new World(1);

        Assert.Throws<ArgumentException>(() => world.Spawn("not a component"));
        Assert.False(world.IsAlive(new Entity(1)));
    }

    private sealed class LambdaSystem(Action<World, TickCommands> update) : ISystem
    {
        public void Update(World world, TickCommands commands) => update(world, commands);
    }

    private sealed class SpawnOnTickSystem : ISystem
    {
        public void Update(World world, TickCommands commands) => commands.Spawn(new Health(1));
    }
}
