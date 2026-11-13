namespace NightCero.Core.Ecs;

/// <summary>Type-erased access to a component column.</summary>
internal interface IColumn
{
    void Set(object component, uint entity);

    void Remove(uint entity);

    IEnumerable<(Entity Entity, object Value)> Snapshot();
}

/// <summary>
/// One component type for all entities. Iteration is in entity id order because the store is a
/// sorted map; that order is part of the determinism contract (ADR 0002).
/// </summary>
internal sealed class Column<T> : IColumn where T : struct
{
    private readonly SortedDictionary<uint, T> _values = [];

    public void Set(T value, uint entity) => _values[entity] = value;

    public void Set(object component, uint entity)
    {
        if (component is not T typed)
        {
            throw new ArgumentException(
                $"{component.GetType()} is not a valid value for a {typeof(T)} column.",
                nameof(component));
        }

        Set(typed, entity);
    }

    public bool TryGet(uint entity, out T value) => _values.TryGetValue(entity, out value!);

    public T Get(uint entity)
    {
        if (!_values.TryGetValue(entity, out var value))
        {
            throw new KeyNotFoundException($"The entity does not have a {typeof(T).Name} component.");
        }

        return value;
    }

    public void Remove(uint entity) => _values.Remove(entity);

    public IEnumerable<(Entity Entity, object Value)> Snapshot()
    {
        foreach (var pair in _values)
        {
            yield return (new Entity(pair.Key), pair.Value);
        }
    }

    public IEnumerable<(uint Entity, T Value)> Items()
    {
        foreach (var pair in _values)
        {
            yield return (pair.Key, pair.Value);
        }
    }
}
