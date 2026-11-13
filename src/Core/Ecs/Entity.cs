namespace NightCero.Core.Ecs;

/// <summary>A handle to a living entity. Ids are issued in order and never reused.</summary>
public readonly record struct Entity(uint Value)
{
    /// <summary>The null entity.</summary>
    public static Entity None => default;

    public bool IsNone => Value == 0;

    public override string ToString() => $"Entity({Value})";
}
