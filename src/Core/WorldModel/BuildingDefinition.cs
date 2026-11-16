namespace NightCero.Core.WorldModel;

/// <summary>
/// The data a town generator needs to place one kind of building: size and floor ranges plus a
/// selection weight. The catalog itself lives in <c>NightCero.Content</c>; the core only knows the
/// shape, so world generation stays free of content and of the engine.
/// </summary>
/// <param name="Kind">The building kind this definition describes.</param>
/// <param name="DisplayName">Human-readable name, used in logs and tooling.</param>
/// <param name="MinWidth">Smallest footprint along X, in metres.</param>
/// <param name="MaxWidth">Largest footprint along X, in metres.</param>
/// <param name="MinDepth">Smallest footprint along Z, in metres.</param>
/// <param name="MaxDepth">Largest footprint along Z, in metres.</param>
/// <param name="MinFloors">Fewest floors.</param>
/// <param name="MaxFloors">Most floors.</param>
/// <param name="Weight">Relative chance of being picked for a generic lot; ignored when unique.</param>
/// <param name="Unique">
/// When true the generator never picks this kind at random; it assigns it once, to a landmark
/// building, so a town has exactly one of it.
/// </param>
public readonly record struct BuildingDefinition(
    BuildingKind Kind,
    string DisplayName,
    int MinWidth,
    int MaxWidth,
    int MinDepth,
    int MaxDepth,
    int MinFloors,
    int MaxFloors,
    int Weight,
    bool Unique = false)
{
    /// <summary>Throws when the definition cannot produce a valid building.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            throw new ArgumentException($"{Kind} needs a display name.", nameof(DisplayName));
        }

        if (MinWidth <= 0 || MaxWidth < MinWidth)
        {
            throw new ArgumentException($"{Kind} has an invalid width range.", nameof(MinWidth));
        }

        if (MinDepth <= 0 || MaxDepth < MinDepth)
        {
            throw new ArgumentException($"{Kind} has an invalid depth range.", nameof(MinDepth));
        }

        if (MinFloors <= 0 || MaxFloors < MinFloors)
        {
            throw new ArgumentException($"{Kind} has an invalid floor range.", nameof(MinFloors));
        }

        if (!Unique && Weight <= 0)
        {
            throw new ArgumentException($"{Kind} is not unique and needs a positive weight.", nameof(Weight));
        }
    }
}
