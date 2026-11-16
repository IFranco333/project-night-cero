using NightCero.Content;
using NightCero.Core.WorldModel;

namespace NightCero.Tests.Content;

public sealed class BuildingCatalogTests
{
    [Fact]
    public void TheDefaultCatalogIsValid()
    {
        BuildingCatalog.Validate();
    }

    [Fact]
    public void EveryKindHasADefinition()
    {
        foreach (var kind in Enum.GetValues<BuildingKind>())
        {
            Assert.Equal(kind, BuildingCatalog.For(kind).Kind);
        }
    }

    [Fact]
    public void DuplicateKindsAreRejected()
    {
        var duplicate = new[] { BuildingCatalog.All[0], BuildingCatalog.All[0] };

        Assert.Throws<InvalidOperationException>(() => BuildingCatalog.Validate(duplicate));
    }

    [Fact]
    public void AMissingKindIsRejected()
    {
        var incomplete = BuildingCatalog.All
            .Where(definition => definition.Kind != BuildingKind.Garage)
            .ToArray();

        Assert.Throws<InvalidOperationException>(() => BuildingCatalog.Validate(incomplete));
    }

    [Fact]
    public void AnInvertedSizeRangeIsRejected()
    {
        var invalid = new BuildingDefinition(
            BuildingKind.House, "House", MinWidth: 20, MaxWidth: 10, MinDepth: 8, MaxDepth: 8,
            MinFloors: 1, MaxFloors: 1, Weight: 1);

        Assert.Throws<ArgumentException>(invalid.Validate);
    }

    [Fact]
    public void AGenericKindWithoutWeightIsRejected()
    {
        var invalid = new BuildingDefinition(
            BuildingKind.House, "House", MinWidth: 8, MaxWidth: 10, MinDepth: 8, MaxDepth: 10,
            MinFloors: 1, MaxFloors: 1, Weight: 0);

        Assert.Throws<ArgumentException>(invalid.Validate);
    }

    [Fact]
    public void AUniqueKindMayHaveNoWeight()
    {
        var unique = new BuildingDefinition(
            BuildingKind.Hospital, "Hospital", MinWidth: 20, MaxWidth: 28, MinDepth: 20, MaxDepth: 28,
            MinFloors: 2, MaxFloors: 3, Weight: 0, Unique: true);

        unique.Validate();
    }

    [Fact]
    public void AnUnknownKindHasNoDefinition()
    {
        Assert.Throws<InvalidOperationException>(() => BuildingCatalog.For((BuildingKind)999));
    }
}
