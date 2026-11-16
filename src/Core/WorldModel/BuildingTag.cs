namespace NightCero.Core.WorldModel;

/// <summary>
/// The simulation-side description of a building: enough for the state hash, the navigation graph
/// and the renderer, without the full generated record (rooms, entrance) that only world
/// generation needs.
/// </summary>
public readonly record struct BuildingTag(BuildingKind Kind, float Width, float Depth, int Floors);

/// <summary>The simulation-side description of a static prop.</summary>
public readonly record struct PropTag(PropKind Kind);
