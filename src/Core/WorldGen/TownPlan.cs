using System.Globalization;
using System.Text;
using NightCero.Core.Math;
using NightCero.Core.WorldModel;

namespace NightCero.Core.WorldGen;

/// <summary>
/// The static layout of a town, generated from a seed. Pure data: no entities, no engine types.
/// </summary>
/// <remarks>
/// <see cref="CanonicalText"/> and <see cref="Hash"/> make the plan checkable: the same seed must
/// produce the same text and the same hash on every platform, which is what the golden tests pin
/// down (ADR 0002).
/// </remarks>
public sealed record TownPlan(
    ulong Seed,
    int Width,
    int Height,
    int BlockPitch,
    int RoadWidth,
    IReadOnlyList<RoadSegment> Roads,
    IReadOnlyList<Block> Blocks,
    IReadOnlyList<Building> Buildings,
    IReadOnlyList<Prop> Props)
{
    /// <summary>The town rectangle on the ground plane.</summary>
    public Rect2 Bounds => new(0, 0, Width, Height);

    /// <summary>The canonical, human-diffable text of the plan.</summary>
    public string CanonicalText()
    {
        var text = new StringBuilder();
        text.Append("seed=").Append(Seed.ToString(CultureInfo.InvariantCulture)).Append('\n');
        text.Append("size=").Append(Width.ToString(CultureInfo.InvariantCulture))
            .Append('x').Append(Height.ToString(CultureInfo.InvariantCulture))
            .Append(" pitch=").Append(BlockPitch.ToString(CultureInfo.InvariantCulture))
            .Append(" road=").Append(RoadWidth.ToString(CultureInfo.InvariantCulture)).Append('\n');

        foreach (var road in Roads)
        {
            text.Append("road ").Append(Format(road.Start)).Append(" -> ").Append(Format(road.End))
                .Append(" w=").Append(Number(road.Width)).Append('\n');
        }

        foreach (var block in Blocks)
        {
            text.Append("block ").Append(block.Kind)
                .Append(" min=").Append(Format(new Vec3(block.Bounds.MinX, 0, block.Bounds.MinY)))
                .Append(" max=").Append(Format(new Vec3(block.Bounds.MaxX, 0, block.Bounds.MaxY)))
                .Append('\n');
        }

        foreach (var building in Buildings)
        {
            text.Append("building ").Append(building.Id.ToString(CultureInfo.InvariantCulture))
                .Append(' ').Append(building.Kind)
                .Append(" c=").Append(Format(building.Centre))
                .Append(" size=").Append(Number(building.Width)).Append('x').Append(Number(building.Depth))
                .Append(" floors=").Append(building.Floors.ToString(CultureInfo.InvariantCulture))
                .Append(" entrance=").Append(Format(building.Entrance)).Append('\n');
            foreach (var room in building.Rooms)
            {
                text.Append("  room ").Append(room.Index.ToString(CultureInfo.InvariantCulture))
                    .Append(" c=").Append(Format(room.Centre))
                    .Append(" size=").Append(Number(room.Width)).Append('x').Append(Number(room.Depth))
                    .Append('\n');
            }
        }

        foreach (var prop in Props)
        {
            text.Append("prop ").Append(prop.Kind)
                .Append(' ').Append(Format(prop.Position))
                .Append(" yaw=").Append(Number(prop.YawDegrees)).Append('\n');
        }

        return text.ToString();
    }

    /// <summary>FNV-1a 64-bit hash of <see cref="CanonicalText"/>.</summary>
    public ulong Hash() => Hashing.Fnv1a64(CanonicalText());

    private static string Format(Vec3 position) =>
        $"({Number(position.X)},{Number(position.Y)},{Number(position.Z)})";

    private static string Number(float value) =>
        value == 0f ? "0" : value.ToString("0.###", CultureInfo.InvariantCulture);
}
