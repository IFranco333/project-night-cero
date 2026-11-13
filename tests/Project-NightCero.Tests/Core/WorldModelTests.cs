using NightCero.Core.WorldModel;
using Vec3 = NightCero.Core.Math.Vec3;

namespace NightCero.Tests.Core;

public sealed class WorldModelTests
{
    [Fact]
    public void ChunkIndexUsesFloorDivisionForNegativeCoordinates()
    {
        Assert.Equal(new ChunkCoord(0, 0), ChunkCoord.FromWorld(new Vec3(0, 0, 0)));
        Assert.Equal(new ChunkCoord(0, 0), ChunkCoord.FromWorld(new Vec3(31.9f, 5, 31.9f)));
        Assert.Equal(new ChunkCoord(1, 1), ChunkCoord.FromWorld(new Vec3(32, 0, 32)));
        Assert.Equal(new ChunkCoord(-1, -1), ChunkCoord.FromWorld(new Vec3(-0.1f, 0, -0.1f)));
        Assert.Equal(new ChunkCoord(-2, 0), ChunkCoord.FromWorld(new Vec3(-33, 0, 0)));
    }

    [Fact]
    public void ChunkCentreIsTheMiddleOfItsSquare()
    {
        var centre = new ChunkCoord(2, 3).Centre;

        Assert.Equal(2 * 32 + 16, centre.X, 5);
        Assert.Equal(0, centre.Y, 5);
        Assert.Equal(3 * 32 + 16, centre.Z, 5);
    }

    [Fact]
    public void Vec3ArithmeticIsExactForSmallIsotropicSteps()
    {
        var position = new Vec3(1, 2, 3) + new Vec3(0.5f, -1, 0.25f);

        Assert.Equal(new Vec3(1.5f, 1, 3.25f), position);
        Assert.Equal(14.75f, position.Dot(new Vec3(2, 2, 3)), 5);
        Assert.Equal(new Vec3(-2, -4, -6), -new Vec3(2, 4, 6));
    }

    [Fact]
    public void DistanceIsComputedInXZWhenRequested()
    {
        Assert.Equal(5, new Vec3(3, 0, 4).LengthXZ, 5);
    }
}
