using NightCero.Core.WorldModel;

namespace NightCero.Content;

/// <summary>
/// The building definitions of the vertical slice. Data drives the town generator: sizes, floor
/// ranges and how often each kind is picked for a generic lot.
/// </summary>
/// <remarks>
/// <see cref="Validate"/> runs at boot and in tests, so a malformed catalog fails before a world is
/// generated. Unique kinds (the church and the hospital) are never picked at random; the generator
/// assigns each one to a single landmark building.
/// </remarks>
public static class BuildingCatalog
{
    /// <summary>Every building definition, in a fixed order.</summary>
    public static IReadOnlyList<BuildingDefinition> All { get; } =
    [
        new(BuildingKind.House, "House", MinWidth: 8, MaxWidth: 16, MinDepth: 8, MaxDepth: 16,
            MinFloors: 1, MaxFloors: 2, Weight: 50),
        new(BuildingKind.Shop, "Shop", MinWidth: 12, MaxWidth: 20, MinDepth: 12, MaxDepth: 20,
            MinFloors: 1, MaxFloors: 1, Weight: 18),
        new(BuildingKind.Warehouse, "Warehouse", MinWidth: 16, MaxWidth: 26, MinDepth: 14, MaxDepth: 24,
            MinFloors: 1, MaxFloors: 2, Weight: 8),
        new(BuildingKind.Garage, "Garage", MinWidth: 6, MaxWidth: 10, MinDepth: 6, MaxDepth: 10,
            MinFloors: 1, MaxFloors: 1, Weight: 12),
        new(BuildingKind.Church, "Church", MinWidth: 12, MaxWidth: 20, MinDepth: 14, MaxDepth: 22,
            MinFloors: 1, MaxFloors: 1, Weight: 0, Unique: true),
        new(BuildingKind.Hospital, "Hospital", MinWidth: 20, MaxWidth: 28, MinDepth: 20, MaxDepth: 28,
            MinFloors: 2, MaxFloors: 3, Weight: 0, Unique: true),
    ];

    /// <summary>The definition of a kind; throws when the catalog has none.</summary>
    public static BuildingDefinition For(BuildingKind kind)
    {
        foreach (var definition in All)
        {
            if (definition.Kind == kind)
            {
                return definition;
            }
        }

        throw new InvalidOperationException($"The building catalog has no definition for {kind}.");
    }

    /// <summary>Throws when the catalog is incomplete or any definition is invalid.</summary>
    /// <param name="catalog">The catalog to check; defaults to <see cref="All"/>.</param>
    public static void Validate(IReadOnlyList<BuildingDefinition>? catalog = null)
    {
        var definitions = catalog ?? All;
        var seen = new HashSet<BuildingKind>();

        foreach (var definition in definitions)
        {
            if (!seen.Add(definition.Kind))
            {
                throw new InvalidOperationException($"Duplicate building definition for {definition.Kind}.");
            }

            definition.Validate();
        }

        foreach (var kind in Enum.GetValues<BuildingKind>())
        {
            if (!seen.Contains(kind))
            {
                throw new InvalidOperationException($"Missing building definition for {kind}.");
            }
        }
    }
}
