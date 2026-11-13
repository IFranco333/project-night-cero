using System.Collections.Concurrent;
using NightCero.Core.Rng;

namespace NightCero.Core.Ecs;

/// <summary>
/// The entity–component store of the simulation.
/// </summary>
/// <remarks>
/// <para>
/// Entities are plain ids issued in increasing order and never reused; components live in one
/// column per type, owned by this world. Queries iterate columns in <see cref="Entity"/> id order,
/// never in hash or insertion order, so every run produces the same sequence of updates (ADR 0002).
/// </para>
/// <para>
/// Structural changes requested by systems during a tick go through <see cref="TickCommands"/> and
/// are applied together by <see cref="Tick"/>. While systems run, the world rejects direct
/// structural changes, so iteration over <see cref="Query{T}"/> is stable for the whole tick.
/// </para>
/// </remarks>
public sealed class World
{
    private static readonly ConcurrentDictionary<Type, bool> IsStructType = new();

    private readonly Dictionary<Type, IColumn> _columns = [];
    private readonly List<bool> _alive = [];
    private readonly HashSet<uint> _pending = [];
    private readonly List<ISystem> _systems = [];
    private readonly Dictionary<Type, List<Subscription>> _subscribers = [];
    private readonly List<object> _published = [];
    private uint _nextEntityId = 1;
    private bool _ticking;

    /// <summary>Creates a world whose random streams derive from <paramref name="seed"/>.</summary>
    public World(ulong seed)
    {
        Seed = seed;
        Rng = new RngStream(seed, "world");
    }

    /// <summary>The world seed every random stream derives from.</summary>
    public ulong Seed { get; }

    /// <summary>The root random stream; fork named children from it.</summary>
    public RngStream Rng { get; }

    /// <summary>Number of ticks completed so far.</summary>
    public long TickCount { get; private set; }

    // ---- Entities -------------------------------------------------------------------------

    /// <summary>Creates an entity and optionally attaches components.</summary>
    public Entity Spawn(params object[] components)
    {
        ArgumentNullException.ThrowIfNull(components);
        EnsureNotTicking("spawn");
        var entity = new Entity(_nextEntityId++);
        AttachAll(entity, components);
        SetAlive(entity, true);
        return entity;
    }

    /// <summary>Whether the entity exists in this world.</summary>
    public bool IsAlive(Entity entity) =>
        entity.Value != 0 && entity.Value <= _alive.Count && _alive[(int)entity.Value - 1]
            && !_pending.Contains(entity.Value);

    /// <summary>Removes an entity and all of its components.</summary>
    public void Despawn(Entity entity)
    {
        EnsureNotTicking("despawn");
        if (!IsAlive(entity))
        {
            return;
        }

        foreach (var column in _columns.Values)
        {
            column.Remove(entity.Value);
        }

        SetAlive(entity, false);
    }

    /// <summary>The highest entity id ever issued in this world.</summary>
    public uint HighestEntityId => _nextEntityId - 1;

    /// <summary>The id the next spawn would receive.</summary>
    internal uint PeekNextEntityId() => _nextEntityId;

    /// <summary>Consumes the next id without spawning (used by the command buffer).</summary>
    internal void AdvanceEntityId() => _nextEntityId++;

    /// <summary>Marks a reserved id as not yet alive; it becomes alive when its spawn applies.</summary>
    internal void MarkPending(Entity entity)
    {
        EnsureAliveSlot(entity);
        _pending.Add(entity.Value);
    }

    /// <summary>Spawns an entity under a previously consumed id.</summary>
    internal void SpawnAs(Entity entity, object[] components)
    {
        AttachAll(entity, components);
        _pending.Remove(entity.Value);
        SetAlive(entity, true);
    }

    // ---- Components -----------------------------------------------------------------------

    /// <summary>Adds or replaces a component on a living entity.</summary>
    public void Set<T>(Entity entity, T value) where T : struct
    {
        EnsureAlive(entity);
        ColumnOf<T>().Set(value, entity.Value);
    }

    /// <summary>Returns a component; throws if the entity does not have it.</summary>
    public T Get<T>(Entity entity) where T : struct
    {
        EnsureAlive(entity);
        return ColumnOf<T>().Get(entity.Value);
    }

    /// <summary>Whether the living entity has the component.</summary>
    public bool Has<T>(Entity entity) where T : struct =>
        IsAlive(entity) && ColumnOf<T>().TryGet(entity.Value, out _);

    /// <summary>Removes a component if present.</summary>
    public void Remove<T>(Entity entity) where T : struct
    {
        EnsureAlive(entity);
        ColumnOf<T>().Remove(entity.Value);
    }

    /// <summary>Iterates living entities that have <typeparamref name="T"/>, in entity id order.</summary>
    public IEnumerable<(Entity Entity, T Value)> Query<T>() where T : struct
    {
        foreach (var (id, value) in ColumnOf<T>().Items())
        {
            if (IsAlive(new Entity(id)))
            {
                yield return (new Entity(id), value);
            }
        }
    }

    /// <summary>
    /// Component columns in a stable order (type name) for hashing and diagnostics. Values are
    /// boxed; systems should not call this every tick.
    /// </summary>
    public IEnumerable<(Type Type, IEnumerable<(Entity Entity, object Value)> Rows)> Columns()
    {
        foreach (var (type, column) in _columns.OrderBy(pair => pair.Key.FullName, StringComparer.Ordinal))
        {
            yield return (type, column.Snapshot());
        }
    }

    // ---- Systems and ticks ----------------------------------------------------------------

    /// <summary>Registers a system. Systems run in registration order.</summary>
    public void AddSystem(ISystem system)
    {
        ArgumentNullException.ThrowIfNull(system);
        _systems.Add(system);
    }

    /// <summary>Runs one tick: systems in registration order, then commands, then events.</summary>
    public void Tick()
    {
        _ticking = true;
        var commands = new TickCommands(this);
        try
        {
            foreach (var system in _systems)
            {
                system.Update(this, commands);
            }
        }
        finally
        {
            _ticking = false;
        }

        commands.Apply();
        DeliverPublished();
        TickCount++;
    }

    // ---- Events ---------------------------------------------------------------------------

    /// <summary>
    /// Subscribes to events assignable to <typeparamref name="T"/> (including derived types and
    /// interface implementations). Delivery happens at the end of the tick.
    /// </summary>
    public void Subscribe<T>(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        SubscribersFor(typeof(T)).Add(new Subscription(handler));
    }

    /// <summary>Removes exactly this handler, previously passed to <see cref="Subscribe{T}"/>.</summary>
    public void Unsubscribe<T>(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        SubscribersFor(typeof(T)).RemoveAll(s => s.Matches(handler));
    }

    /// <summary>Publishes an event. Subscribers receive it at the end of the current tick.</summary>
    public void Publish<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _published.Add(value);
    }

    /// <summary>Events published during the current tick, in publication order.</summary>
    public IEnumerable<T> Published<T>() => _published.OfType<T>();

    // ---- Internals ------------------------------------------------------------------------

    private void EnsureNotTicking(string operation)
    {
        if (_ticking)
        {
            throw new InvalidOperationException(
                $"Cannot {operation} while systems run; use the TickCommands buffer instead.");
        }
    }

    private void EnsureAlive(Entity entity)
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"{entity} is not alive.");
        }
    }

    private void SetAlive(Entity entity, bool alive)
    {
        EnsureAliveSlot(entity);
        _alive[(int)entity.Value - 1] = alive;
    }

    private void EnsureAliveSlot(Entity entity)
    {
        while (_alive.Count < entity.Value)
        {
            _alive.Add(false);
        }
    }

    private void AttachAll(Entity entity, object[] components)
    {
        foreach (var component in components)
        {
            ArgumentNullException.ThrowIfNull(component);
            if (!IsStructType.GetOrAdd(component.GetType(), static t => t.IsValueType && !t.IsEnum))
            {
                throw new ArgumentException(
                    $"Components must be value types; {component.GetType().Name} is not.",
                    nameof(components));
            }

            ColumnFor(component).Set(component, entity.Value);
        }
    }

    /// <summary>The column for a value type, registered with this world so Despawn can clear it.</summary>
    private Column<T> ColumnOf<T>()
        where T : struct =>
        (Column<T>)(_columns.TryGetValue(typeof(T), out var column)
            ? column
            : _columns[typeof(T)] = new Column<T>());

    private IColumn ColumnFor(object component)
    {
        var type = component.GetType();
        if (!_columns.TryGetValue(type, out var column))
        {
            column = (IColumn)Activator.CreateInstance(typeof(Column<>).MakeGenericType(type))!;
            _columns[type] = column;
        }

        return column;
    }

    private List<Subscription> SubscribersFor(Type type)
    {
        if (!_subscribers.TryGetValue(type, out var list))
        {
            list = [];
            _subscribers[type] = list;
        }

        return list;
    }

    /// <summary>Delivers pending events to every subscriber whose event type accepts an ancestor type.</summary>
    private void DeliverPublished()
    {
        if (_published.Count == 0)
        {
            return;
        }

        var pending = _published.ToArray();
        _published.Clear();
        foreach (var value in pending)
        {
            foreach (var (type, subscribers) in _subscribers)
            {
                if (!type.IsInstanceOfType(value))
                {
                    continue;
                }

                // Snapshot so a handler can subscribe or unsubscribe while events are delivered.
                foreach (var subscription in subscribers.ToArray())
                {
                    subscription.Invoke(value);
                }
            }
        }
    }

}
