using NightCero.Content;
using NightCero.Core.Math;
using NightCero.Core.Simulation;
using NightCero.Core.WorldGen;
using NightCero.Core.WorldModel;
using NightCero.Tests.Support;

namespace NightCero.Tests.WorldGen;

public sealed class TownGeneratorTests
{
    [Fact]
    public void SameSeedProducesTheSamePlan()
    {
        var a = TownFixtures.Plan(2026);
        var b = TownFixtures.Plan(2026);

        Assert.Equal(a.CanonicalText(), b.CanonicalText());
        Assert.Equal(a.Hash(), b.Hash());
    }

    [Fact]
    public void DifferentSeedsProduceDifferentPlans()
    {
        Assert.NotEqual(TownFixtures.Plan(2026).Hash(), TownFixtures.Plan(2027).Hash());
    }

    /// <summary>
    /// Pins the generated town itself. A change here invalidates every golden town hash and every
    /// save that stores one: bump the save version and write a migration (ADR 0002).
    /// </summary>
    [Fact]
    public void GoldenHashIsStableForSeed2026()
    {
        Assert.Equal(8270809061421129897UL, TownFixtures.Plan(2026).Hash());
    }

    [Fact]
    public void BuildingsStayInsideTheTownAndDoNotOverlap()
    {
        var plan = TownFixtures.Plan();
        Assert.NotEmpty(plan.Buildings);

        for (var i = 0; i < plan.Buildings.Count; i++)
        {
            var building = plan.Buildings[i];
            var footprint = building.Footprint;

            Assert.InRange(building.Width, 1, plan.Width);
            Assert.InRange(building.Depth, 1, plan.Height);
            Assert.True(plan.Bounds.Contains(new Vec2(footprint.MinX, footprint.MinY)));
            Assert.True(plan.Bounds.Contains(new Vec2(footprint.MaxX, footprint.MaxY)));
            Assert.NotEmpty(building.Rooms);

            for (var j = i + 1; j < plan.Buildings.Count; j++)
            {
                Assert.False(
                    footprint.Intersects(plan.Buildings[j].Footprint),
                    $"building {building.Id} overlaps building {plan.Buildings[j].Id}");
            }
        }
    }

    [Fact]
    public void EveryEntranceSitsOnItsBuildingWall()
    {
        foreach (var building in TownFixtures.Plan().Buildings)
        {
            var footprint = building.Footprint;
            var x = building.Entrance.X;
            var z = building.Entrance.Z;

            var onWestOrEast = (Near(x, footprint.MinX) || Near(x, footprint.MaxX))
                && z >= footprint.MinY && z <= footprint.MaxY;
            var onNorthOrSouth = (Near(z, footprint.MinY) || Near(z, footprint.MaxY))
                && x >= footprint.MinX && x <= footprint.MaxX;

            Assert.True(onWestOrEast || onNorthOrSouth, $"entrance of building {building.Id} is off its wall");
        }
    }

    [Fact]
    public void EveryRoomSitsInsideItsBuilding()
    {
        foreach (var building in TownFixtures.Plan().Buildings)
        {
            foreach (var room in building.Rooms)
            {
                var roomFootprint = Rect2.FromCentre(new Vec2(room.Centre.X, room.Centre.Z), room.Width, room.Depth);
                Assert.True(building.Footprint.Intersects(roomFootprint) || room.Width == 0);
                Assert.True(room.Width > 0 && room.Depth > 0);
            }
        }
    }

    [Fact]
    public void ThePlanHasExactlyOneOfEveryLandmark()
    {
        var plan = TownFixtures.Plan();

        Assert.Single(plan.Buildings, building => building.Kind == BuildingKind.Hospital);
        Assert.Single(plan.Buildings, building => building.Kind == BuildingKind.Church);
        Assert.Single(plan.Buildings, building => building.Kind == BuildingKind.Police);
    }

    [Fact]
    public void TheBiggestBuildingIsTheHospital()
    {
        var plan = TownFixtures.Plan();
        var biggest = plan.Buildings.MaxBy(building => building.Width * building.Depth);

        Assert.Equal(BuildingKind.Hospital, biggest.Kind);
    }

    [Fact]
    public void ExactlyOneHouseHidesTheCatacombEntrance()
    {
        var plan = TownFixtures.Plan();

        var entrance = Assert.Single(plan.Buildings, building => building.CatacombEntrance);
        Assert.Contains(entrance.Kind, new[] { BuildingKind.House, BuildingKind.Mansion });
    }

    [Fact]
    public void BasementsOnlyAppearUnderHousesAndMansions()
    {
        var plan = TownFixtures.Plan();
        var basements = plan.Buildings.Where(building => building.HasBasement).ToArray();

        Assert.NotEmpty(basements);
        Assert.All(basements, building =>
            Assert.Contains(building.Kind, new[] { BuildingKind.House, BuildingKind.Mansion }));
    }

    [Fact]
    public void EveryForestBlockIsWoodedAndHasNoBuildings()
    {
        // The slice town, not the small fixture: 16 blocks are too few to guarantee a wood.
        var plan = TownGenerator.Generate(2026, TownSettings.Default, BuildingCatalog.All);
        var forests = plan.Blocks.Where(block => block.Kind == BlockKind.Forest).ToArray();
        Assert.NotEmpty(forests);

        foreach (var forest in forests)
        {
            var trees = plan.Props.Count(prop =>
                prop.Kind == PropKind.Tree
                && forest.Bounds.Contains(new Vec2(prop.Position.X, prop.Position.Z)));

            Assert.True(trees >= 40, $"forest block at {forest.Bounds.MinX},{forest.Bounds.MinY} has only {trees} trees");
            Assert.DoesNotContain(
                plan.Buildings,
                building => forest.Bounds.Contains(new Vec2(building.Centre.X, building.Centre.Z)));
        }
    }

    [Fact]
    public void ThePlanHasOneBlockPerGridCell()
    {
        var settings = TownFixtures.Small;
        var plan = TownFixtures.Plan();

        Assert.Equal(settings.BlocksX * settings.BlocksZ, plan.Blocks.Count);
        Assert.All(plan.Blocks, block => Assert.True(block.Bounds.Width > 0 && block.Bounds.Height > 0));
    }

    [Fact]
    public void EveryParkBlockHasTreesAndNoBuildings()
    {
        var plan = TownFixtures.Plan();
        var parks = plan.Blocks.Where(block => block.Kind == BlockKind.Park).ToArray();
        Assert.NotEmpty(parks);

        foreach (var park in parks)
        {
            Assert.Contains(
                plan.Props,
                prop => prop.Kind == PropKind.Tree
                    && park.Bounds.Contains(new Vec2(prop.Position.X, prop.Position.Z)));
            Assert.DoesNotContain(
                plan.Buildings,
                building => park.Bounds.Contains(new Vec2(building.Centre.X, building.Centre.Z)));
        }
    }

    [Fact]
    public void EveryBuiltBlockHoldsAtLeastOneBuilding()
    {
        var plan = TownFixtures.Plan();
        var built = plan.Blocks.Where(block => block.Kind == BlockKind.Built).ToArray();
        Assert.NotEmpty(built);

        foreach (var block in built)
        {
            Assert.Contains(
                plan.Buildings,
                building => block.Bounds.Contains(new Vec2(building.Centre.X, building.Centre.Z)));
        }
    }

    [Fact]
    public void EmptyBlocksHoldNoBuildings()
    {
        var plan = TownFixtures.Plan();

        foreach (var block in plan.Blocks.Where(block => block.Kind == BlockKind.Empty))
        {
            Assert.DoesNotContain(
                plan.Buildings,
                building => block.Bounds.Contains(new Vec2(building.Centre.X, building.Centre.Z)));
        }
    }

    [Fact]
    public void TheRoadGridClosesOnItself()
    {
        var settings = TownFixtures.Small;
        var plan = TownFixtures.Plan();

        Assert.Equal((settings.BlocksX + 1) + (settings.BlocksZ + 1), plan.Roads.Count);
        Assert.All(plan.Roads, road => Assert.True(road.Length > 0));
        Assert.Equal(plan.Roads.Count, plan.Roads.Count(road => road.Width == settings.RoadWidth));
    }

    [Fact]
    public void SpawningTheSameTownTwiceProducesTheSameStateHash()
    {
        var a = new Simulation(2026);
        TownSpawner.Spawn(a.World, TownFixtures.Plan(2026));

        var b = new Simulation(2026);
        TownSpawner.Spawn(b.World, TownFixtures.Plan(2026));

        Assert.NotEqual(0u, a.World.HighestEntityId);
        Assert.Equal(a.StateHash(), b.StateHash());
    }

    [Fact]
    public void TheTownContainsBuildingsAndProps()
    {
        var plan = TownFixtures.Plan();

        Assert.NotEmpty(plan.Buildings);
        Assert.NotEmpty(plan.Props);
        Assert.Contains(plan.Props, prop => prop.Kind == PropKind.Streetlight);
        Assert.Contains(plan.Props, prop => prop.Kind == PropKind.Tree);
    }

    [Fact]
    public void DefaultSettingsDescribeTheSliceTown()
    {
        var settings = TownSettings.Default;

        Assert.Equal(1024, settings.Width);
        Assert.Equal(1024, settings.Height);
        Assert.Equal(16, settings.BlocksX);
        settings.Validate();
    }

    [Fact]
    public void SettingsMustBeAMultipleOfTheBlockPitch()
    {
        var settings = new TownSettings(Width: 200, Height: 192, BlockPitch: 48, RoadWidth: 8, BuildingMargin: 2);

        Assert.Throws<ArgumentException>(() => TownGenerator.Generate(1, settings, BuildingCatalog.All));
    }

    [Fact]
    public void RoadsWiderThanHalfABlockAreRejected()
    {
        var settings = new TownSettings(Width: 192, Height: 192, BlockPitch: 48, RoadWidth: 24, BuildingMargin: 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => TownGenerator.Generate(1, settings, BuildingCatalog.All));
    }

    [Fact]
    public void EmptyCatalogIsRejected()
    {
        Assert.Throws<ArgumentException>(() => TownGenerator.Generate(1, TownFixtures.Small, []));
    }

    [Fact]
    public void CatalogWithoutGenericBuildingsIsRejected()
    {
        var uniqueOnly = BuildingCatalog.All.Where(definition => definition.Unique).ToArray();

        Assert.Throws<ArgumentException>(() => TownGenerator.Generate(1, TownFixtures.Small, uniqueOnly));
    }

    private static bool Near(float a, float b) => MathF.Abs(a - b) < 0.001f;
}
