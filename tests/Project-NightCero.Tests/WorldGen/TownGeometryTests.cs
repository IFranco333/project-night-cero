using NightCero.Core.Math;
using NightCero.Core.WorldGen;
using NightCero.Core.WorldModel;

namespace NightCero.Tests.WorldGen;

public sealed class TownGeometryTests
{
    [Fact]
    public void TheInsideOfABuildingIsNotWalkable()
    {
        var plan = SingleBuildingPlan();

        Assert.False(TownGeometry.IsWalkable(plan, new Vec3(50, 0, 50)));
        Assert.True(TownGeometry.IsWalkable(plan, new Vec3(20, 0, 20)));
    }

    [Fact]
    public void OutsideTheTownIsNotWalkable()
    {
        var plan = SingleBuildingPlan();

        Assert.False(TownGeometry.IsWalkable(plan, new Vec3(-1, 0, 20)));
        Assert.False(TownGeometry.IsWalkable(plan, new Vec3(20, 0, 101)));
    }

    [Fact]
    public void TheRadiusGrowsTheBuildingFootprint()
    {
        var plan = SingleBuildingPlan();

        // The building spans x in [45, 55]; a 2 m body at x = 56.5 touches it.
        Assert.True(TownGeometry.IsWalkable(plan, new Vec3(56.5f, 0, 50)));
        Assert.False(TownGeometry.IsWalkable(plan, new Vec3(56.5f, 0, 50), radius: 2f));
    }

    [Fact]
    public void AStraightLineThroughABuildingIsNotClear()
    {
        var plan = SingleBuildingPlan();

        Assert.False(TownGeometry.IsSegmentClear(plan, new Vec3(40, 0, 50), new Vec3(60, 0, 50)));
    }

    [Fact]
    public void AStraightLineBesideABuildingIsClear()
    {
        var plan = SingleBuildingPlan();

        Assert.True(TownGeometry.IsSegmentClear(plan, new Vec3(40, 0, 40), new Vec3(60, 0, 40)));
    }

    private static TownPlan SingleBuildingPlan()
    {
        var building = new Building(
            Id: 0,
            Kind: BuildingKind.House,
            Centre: new Vec3(50, 0, 50),
            Width: 10,
            Depth: 10,
            Floors: 1,
            Entrance: new Vec3(50, 0, 55),
            Rooms: []);

        return new TownPlan(
            Seed: 1,
            Width: 100,
            Height: 100,
            BlockPitch: 50,
            RoadWidth: 8,
            Roads: [],
            Buildings: [building],
            Props: []);
    }
}
