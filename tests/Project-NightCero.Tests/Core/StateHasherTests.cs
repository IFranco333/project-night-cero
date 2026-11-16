using NightCero.Core.Ecs;
using NightCero.Core.Simulation;
using NightCero.Core.WorldModel;

namespace NightCero.Tests.Core;

public sealed class StateHasherTests
{
    [Fact]
    public void ChangingAnEnumComponentChangesTheHash()
    {
        var house = new World(1);
        house.Spawn(new BuildingTag(BuildingKind.House, 10, 10, 1));

        var shop = new World(1);
        shop.Spawn(new BuildingTag(BuildingKind.Shop, 10, 10, 1));

        Assert.NotEqual(StateHasher.Hash(house), StateHasher.Hash(shop));
    }

    [Fact]
    public void ChangingAnIntegerComponentChangesTheHash()
    {
        var oneFloor = new World(1);
        oneFloor.Spawn(new BuildingTag(BuildingKind.House, 10, 10, 1));

        var twoFloors = new World(1);
        twoFloors.Spawn(new BuildingTag(BuildingKind.House, 10, 10, 2));

        Assert.NotEqual(StateHasher.Hash(oneFloor), StateHasher.Hash(twoFloors));
    }

    [Fact]
    public void TheCanonicalTextIncludesPrimitiveValues()
    {
        var world = new World(1);
        world.Spawn(new BuildingTag(BuildingKind.House, 10, 10, 2));

        var text = StateHasher.Canonical(world);

        Assert.Contains("Kind=0", text);
        Assert.Contains("Floors=2", text);
        Assert.Contains("Width=10", text);
    }

    [Fact]
    public void TheSameStateHashesTheSameWay()
    {
        var a = new World(7);
        a.Spawn(new BuildingTag(BuildingKind.House, 10, 10, 1), new PropTag(PropKind.Tree));

        var b = new World(7);
        b.Spawn(new BuildingTag(BuildingKind.House, 10, 10, 1), new PropTag(PropKind.Tree));

        Assert.Equal(StateHasher.Hash(a), StateHasher.Hash(b));
    }
}
