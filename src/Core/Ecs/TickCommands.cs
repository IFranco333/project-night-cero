namespace NightCero.Core.Ecs;

/// <summary>
/// The command buffer of a tick: structural changes requested by systems and applied together
/// at the end of the tick, in the order they were requested.
/// </summary>
/// <remarks>
/// Commands on an entity that has already been despawned by an earlier command in the same tick
/// are skipped instead of failing the tick, so the order in which systems request a kill and a
/// follow-up change on the killed entity does not matter.
/// </remarks>
public sealed class TickCommands
{
    private readonly World _world;
    private readonly List<Action> _pending = [];

    internal TickCommands(World world) => _world = world;

    /// <summary>Number of changes waiting to be applied.</summary>
    public int Count => _pending.Count;

    /// <summary>Requests a spawn. The returned handle becomes alive when the commands apply.</summary>
    public Entity Spawn(params object[] components)
    {
        ArgumentNullException.ThrowIfNull(components);
        var entity = new Entity(_world.PeekNextEntityId());
        _world.AdvanceEntityId();
        _world.MarkPending(entity);
        _pending.Add(() => _world.SpawnAs(entity, components));
        return entity;
    }

    /// <summary>Requests that an entity and its components be removed.</summary>
    public void Despawn(Entity entity) => _pending.Add(() => _world.Despawn(entity));

    /// <summary>Requests that a component be added or replaced.</summary>
    public void Set<T>(Entity entity, T value) where T : struct =>
        _pending.Add(() =>
        {
            if (_world.IsAlive(entity))
            {
                _world.Set(entity, value);
            }
        });

    /// <summary>Requests that a component be removed.</summary>
    public void Remove<T>(Entity entity) where T : struct =>
        _pending.Add(() =>
        {
            if (_world.IsAlive(entity))
            {
                _world.Remove<T>(entity);
            }
        });

    internal void Apply()
    {
        for (var i = 0; i < _pending.Count; i++)
        {
            _pending[i]();
        }

        _pending.Clear();
    }
}
