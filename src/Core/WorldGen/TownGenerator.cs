using NightCero.Core.Math;
using NightCero.Core.Rng;
using NightCero.Core.WorldModel;

namespace NightCero.Core.WorldGen;

/// <summary>
/// Generates a town layout from a seed: a road grid, buildings inside the blocks and static props.
/// </summary>
/// <remarks>
/// <para>
/// Every random draw comes from a named stream forked from the seed (<c>town/blocks</c>,
/// <c>town/buildings</c>, <c>town/props</c>), and blocks are visited in row-major order, so the same
/// seed always produces the same plan (ADR 0002). The generator never reads the clock and never
/// uses <see cref="System.Random"/>.
/// </para>
/// <para>
/// The catalog is passed in rather than referenced: the core does not depend on
/// <c>NightCero.Content</c>, which keeps world generation testable with a hand-written catalog.
/// </para>
/// </remarks>
public static class TownGenerator
{
    /// <summary>Name of the root random stream of world generation.</summary>
    public const string StreamName = "town";

    /// <summary>Generates the plan for <paramref name="seed"/>.</summary>
    public static TownPlan Generate(ulong seed, TownSettings settings, IReadOnlyList<BuildingDefinition> catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        settings.Validate();

        if (catalog.Count == 0)
        {
            throw new ArgumentException("The building catalog is empty.", nameof(catalog));
        }

        foreach (var definition in catalog)
        {
            definition.Validate();
        }

        var generic = catalog.Where(d => !d.Unique).OrderBy(d => d.Kind).ToArray();
        if (generic.Length == 0)
        {
            throw new ArgumentException("The catalog needs at least one non-unique building.", nameof(catalog));
        }

        var root = new RngStream(seed, StreamName);
        var blocksRng = root.Fork("blocks");
        var buildingsRng = root.Fork("buildings");
        var propsRng = root.Fork("props");
        var featuresRng = root.Fork("features");

        var roads = BuildRoads(settings);
        var blocks = new List<Block>();
        var buildings = new List<Building>();
        var props = new List<Prop>();

        for (var j = 0; j < settings.BlocksZ; j++)
        {
            for (var i = 0; i < settings.BlocksX; i++)
            {
                var block = BlockRect(i, j, settings);
                var roll = blocksRng.NextDouble();

                if (roll < 0.10)
                {
                    blocks.Add(new Block(block, BlockKind.Park));
                    AddParkProps(props, block, propsRng);
                    continue;
                }

                if (roll < 0.16)
                {
                    blocks.Add(new Block(block, BlockKind.Forest));
                    AddForestProps(props, block, propsRng);
                    continue;
                }

                if (roll < 0.22)
                {
                    blocks.Add(new Block(block, BlockKind.Empty));
                    continue;
                }

                blocks.Add(new Block(block, BlockKind.Built));
                AddBlockBuildings(buildings, block, settings, generic, buildingsRng);

                if (propsRng.NextDouble() < 0.4)
                {
                    props.Add(new Prop(
                        PropKind.Dumpster,
                        new Vec3(block.MinX + 1.5f, 0, block.MaxY - 1.5f),
                        0));
                }
            }
        }

        AssignUniqueKinds(buildings, catalog);
        AssignFeatures(buildings, settings, featuresRng);
        AddStreetProps(props, settings, propsRng);

        return new TownPlan(
            seed,
            settings.Width,
            settings.Height,
            settings.BlockPitch,
            settings.RoadWidth,
            roads,
            blocks,
            buildings,
            props);
    }

    private static List<RoadSegment> BuildRoads(TownSettings settings)
    {
        var roads = new List<RoadSegment>();

        for (var i = 0; i <= settings.BlocksX; i++)
        {
            var x = i * settings.BlockPitch;
            roads.Add(new RoadSegment(new Vec3(x, 0, 0), new Vec3(x, 0, settings.Height), settings.RoadWidth));
        }

        for (var j = 0; j <= settings.BlocksZ; j++)
        {
            var z = j * settings.BlockPitch;
            roads.Add(new RoadSegment(new Vec3(0, 0, z), new Vec3(settings.Width, 0, z), settings.RoadWidth));
        }

        return roads;
    }

    /// <summary>The buildable square of block <c>(i, j)</c>, inset by half a road on every side.</summary>
    private static Rect2 BlockRect(int i, int j, TownSettings settings)
    {
        var half = settings.RoadWidth / 2f;
        var size = settings.BlockPitch - settings.RoadWidth;
        var minX = (i * settings.BlockPitch) + half;
        var minZ = (j * settings.BlockPitch) + half;
        return new Rect2(minX, minZ, minX + size, minZ + size);
    }

    private static void AddBlockBuildings(
        List<Building> buildings,
        Rect2 block,
        TownSettings settings,
        IReadOnlyList<BuildingDefinition> catalog,
        RngStream rng)
    {
        var cellWidth = block.Width / 2f;
        var cellDepth = block.Height / 2f;

        for (var cz = 0; cz < 2; cz++)
        {
            for (var cx = 0; cx < 2; cx++)
            {
                if (rng.NextDouble() > 0.85)
                {
                    continue;
                }

                var cell = new Rect2(
                    block.MinX + (cx * cellWidth),
                    block.MinY + (cz * cellDepth),
                    block.MinX + ((cx + 1) * cellWidth),
                    block.MinY + ((cz + 1) * cellDepth)).Expand(-settings.BuildingMargin);

                if (cell.Width < 4 || cell.Height < 4)
                {
                    continue;
                }

                var definition = Pick(catalog, rng);
                var width = System.Math.Min(rng.NextInt(definition.MinWidth, definition.MaxWidth), (int)cell.Width);
                var depth = System.Math.Min(rng.NextInt(definition.MinDepth, definition.MaxDepth), (int)cell.Height);
                if (width < 2 || depth < 2)
                {
                    continue;
                }

                var slackX = cell.Width - width;
                var slackZ = cell.Height - depth;
                var offsetX = slackX > 0 ? rng.NextInt(0, (int)slackX) - (slackX / 2f) : 0f;
                var offsetZ = slackZ > 0 ? rng.NextInt(0, (int)slackZ) - (slackZ / 2f) : 0f;
                var centre = new Vec2(cell.Centre.X + offsetX, cell.Centre.Y + offsetZ);
                var footprint = Rect2.FromCentre(centre, width, depth);

                buildings.Add(new Building(
                    buildings.Count,
                    definition.Kind,
                    new Vec3(centre.X, 0, centre.Y),
                    width,
                    depth,
                    rng.NextInt(definition.MinFloors, definition.MaxFloors),
                    EntranceFor(footprint, block),
                    RoomsFor(footprint)));
            }
        }
    }

    private static BuildingDefinition Pick(IReadOnlyList<BuildingDefinition> catalog, RngStream rng)
    {
        var total = 0;
        foreach (var definition in catalog)
        {
            total += definition.Weight;
        }

        var roll = rng.NextInt(1, total);
        foreach (var definition in catalog)
        {
            roll -= definition.Weight;
            if (roll <= 0)
            {
                return definition;
            }
        }

        return catalog[^1];
    }

    /// <summary>
    /// The midpoint of the building side that faces the nearest block edge, so the door opens onto
    /// the street rather than into a neighbour.
    /// </summary>
    private static Vec3 EntranceFor(Rect2 footprint, Rect2 block)
    {
        var toWest = footprint.MinX - block.MinX;
        var toEast = block.MaxX - footprint.MaxX;
        var toNorth = footprint.MinY - block.MinY;
        var toSouth = block.MaxY - footprint.MaxY;
        var nearest = MathF.Min(MathF.Min(toWest, toEast), MathF.Min(toNorth, toSouth));

        if (nearest == toNorth)
        {
            return new Vec3(footprint.Centre.X, 0, footprint.MinY);
        }

        if (nearest == toSouth)
        {
            return new Vec3(footprint.Centre.X, 0, footprint.MaxY);
        }

        return nearest == toWest
            ? new Vec3(footprint.MinX, 0, footprint.Centre.Y)
            : new Vec3(footprint.MaxX, 0, footprint.Centre.Y);
    }

    /// <summary>Splits the interior into rooms of roughly 9 m, leaving a half-metre wall.</summary>
    private static IReadOnlyList<Room> RoomsFor(Rect2 footprint)
    {
        const float wall = 0.5f;
        var inner = footprint.Expand(-wall);
        if (inner.Width <= 0 || inner.Height <= 0)
        {
            return [];
        }

        var roomsX = System.Math.Max(1, (int)(inner.Width / 9f));
        var roomsZ = System.Math.Max(1, (int)(inner.Height / 9f));
        var width = inner.Width / roomsX;
        var depth = inner.Height / roomsZ;
        var rooms = new List<Room>(roomsX * roomsZ);

        for (var z = 0; z < roomsZ; z++)
        {
            for (var x = 0; x < roomsX; x++)
            {
                rooms.Add(new Room(
                    rooms.Count,
                    new Vec3(inner.MinX + ((x + 0.5f) * width), 0, inner.MinY + ((z + 0.5f) * depth)),
                    width,
                    depth));
            }
        }

        return rooms;
    }

    /// <summary>
    /// Gives each unique kind to one landmark building. Landmarks are the largest structures in
    /// town, with ties broken by id, so the assignment is stable for a given plan. The uniques are
    /// ordered by the footprint they need, so the biggest building becomes the hospital rather than
    /// the church.
    /// </summary>
    private static void AssignUniqueKinds(List<Building> buildings, IReadOnlyList<BuildingDefinition> catalog)
    {
        var unique = catalog
            .Where(d => d.Unique)
            .OrderByDescending(d => d.MaxWidth * d.MaxDepth)
            .ThenBy(d => d.Kind)
            .ToArray();
        if (unique.Length == 0 || buildings.Count == 0)
        {
            return;
        }

        var candidates = Enumerable.Range(0, buildings.Count)
            .OrderByDescending(index => buildings[index].Width * buildings[index].Depth)
            .ThenBy(index => index)
            .ToArray();

        for (var k = 0; k < unique.Length && k < candidates.Length; k++)
        {
            var index = candidates[k];
            var definition = unique[k];
            var building = buildings[index];
            buildings[index] = building with
            {
                Kind = definition.Kind,
                Floors = System.Math.Clamp(building.Floors, definition.MinFloors, definition.MaxFloors),
            };
        }
    }

    /// <summary>
    /// Adds the features that only some buildings have: a cellar under many houses, and the single
    /// stair down to the catacombs.
    /// </summary>
    /// <remarks>
    /// These draws go to their own stream so that adding a feature never shifts the stream that
    /// places buildings. The catacomb entrance is chosen by distance to the town centre instead of
    /// by a roll, so the way underground is always near where the player starts.
    /// </remarks>
    private static void AssignFeatures(List<Building> buildings, TownSettings settings, RngStream rng)
    {
        for (var i = 0; i < buildings.Count; i++)
        {
            var building = buildings[i];
            if (building.Kind is BuildingKind.House or BuildingKind.Mansion && rng.NextDouble() < 0.35)
            {
                buildings[i] = building with { HasBasement = true };
            }
        }

        var centreX = settings.Width / 2f;
        var centreZ = settings.Height / 2f;
        var candidates = Enumerable.Range(0, buildings.Count)
            .OrderBy(index =>
            {
                var position = buildings[index].Centre;
                var dx = position.X - centreX;
                var dz = position.Z - centreZ;
                return (dx * dx) + (dz * dz);
            })
            .ThenBy(index => index)
            .ToArray();

        if (candidates.Length == 0)
        {
            return;
        }

        // Houses first: a cellar door under a home is the way in; any building is the fallback when
        // the seed produced no house at all.
        var house = candidates
            .Where(index => buildings[index].Kind is BuildingKind.House or BuildingKind.Mansion)
            .Select(index => (int?)index)
            .FirstOrDefault();
        var entrance = house ?? candidates[0];

        buildings[entrance] = buildings[entrance] with { CatacombEntrance = true };
    }

    private static void AddParkProps(List<Prop> props, Rect2 block, RngStream rng)
    {
        var inner = block.Expand(-1.5f);
        var count = rng.NextInt(3, 9);

        for (var i = 0; i < count; i++)
        {
            var x = inner.MinX + ((float)rng.NextDouble() * inner.Width);
            var z = inner.MinY + ((float)rng.NextDouble() * inner.Height);
            props.Add(new Prop(PropKind.Tree, new Vec3(x, 0, z), rng.NextInt(0, 359)));
        }

        if (rng.NextDouble() < 0.5)
        {
            props.Add(new Prop(
                PropKind.Bench,
                new Vec3(block.Centre.X, 0, block.Centre.Y),
                rng.NextInt(0, 3) * 90f));
        }
    }

    /// <summary>Fills a wood: far more trees than a park, and nothing else.</summary>
    private static void AddForestProps(List<Prop> props, Rect2 block, RngStream rng)
    {
        var inner = block.Expand(-2f);
        var count = rng.NextInt(40, 80);

        for (var i = 0; i < count; i++)
        {
            var x = inner.MinX + ((float)rng.NextDouble() * inner.Width);
            var z = inner.MinY + ((float)rng.NextDouble() * inner.Height);
            props.Add(new Prop(PropKind.Tree, new Vec3(x, 0, z), rng.NextInt(0, 359)));
        }
    }

    private static void AddStreetProps(List<Prop> props, TownSettings settings, RngStream rng)
    {
        for (var i = 0; i <= settings.BlocksX; i++)
        {
            for (var j = 0; j <= settings.BlocksZ; j++)
            {
                if (rng.NextDouble() >= 0.6)
                {
                    continue;
                }

                var x = (i * settings.BlockPitch) + (settings.RoadWidth * 0.5f) + 1f;
                var z = (j * settings.BlockPitch) + (settings.RoadWidth * 0.5f) + 1f;
                props.Add(new Prop(PropKind.Streetlight, new Vec3(x, 0, z), 0));
            }
        }

        const int spacing = 24;
        for (var i = 0; i <= settings.BlocksX; i++)
        {
            for (var j = 0; j < settings.BlocksZ; j++)
            {
                var start = (j * settings.BlockPitch) + settings.RoadWidth;
                var end = ((j + 1) * settings.BlockPitch) - settings.RoadWidth;

                for (var k = 1; start + (k * spacing) < end; k++)
                {
                    if (rng.NextDouble() >= 0.18)
                    {
                        continue;
                    }

                    var x = (i * settings.BlockPitch) + (settings.RoadWidth * 0.25f);
                    props.Add(new Prop(PropKind.Car, new Vec3(x, 0, start + (k * spacing)), 0));
                }
            }
        }
    }
}
