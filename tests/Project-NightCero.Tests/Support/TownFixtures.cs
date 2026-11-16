using NightCero.Content;
using NightCero.Core.WorldGen;

namespace NightCero.Tests.Support;

/// <summary>A small town that generates fast but still exercises every placement rule.</summary>
public static class TownFixtures
{
    /// <summary>Four by four blocks of 48 m, with an 8 m road grid.</summary>
    public static TownSettings Small =>
        new(Width: 192, Height: 192, BlockPitch: 48, RoadWidth: 8, BuildingMargin: 2);

    /// <summary>Generates the small town for a seed.</summary>
    public static TownPlan Plan(ulong seed = 2026) =>
        TownGenerator.Generate(seed, Small, BuildingCatalog.All);
}
