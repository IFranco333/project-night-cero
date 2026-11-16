using NightCero.Core.Math;

namespace NightCero.Tests.Core;

public sealed class Rect2Tests
{
    [Fact]
    public void FromCentreBuildsTheExpectedBounds()
    {
        var rect = Rect2.FromCentre(new Vec2(10, 20), width: 4, height: 6);

        Assert.Equal(8, rect.MinX, 5);
        Assert.Equal(17, rect.MinY, 5);
        Assert.Equal(12, rect.MaxX, 5);
        Assert.Equal(23, rect.MaxY, 5);
        Assert.Equal(4, rect.Width, 5);
        Assert.Equal(6, rect.Height, 5);
        Assert.Equal(new Vec2(10, 20), rect.Centre);
    }

    [Fact]
    public void ContainsIncludesTheBoundary()
    {
        var rect = new Rect2(0, 0, 10, 10);

        Assert.True(rect.Contains(new Vec2(0, 0)));
        Assert.True(rect.Contains(new Vec2(10, 10)));
        Assert.False(rect.Contains(new Vec2(10.1f, 5)));
    }

    [Fact]
    public void TouchingRectanglesDoNotIntersect()
    {
        var a = new Rect2(0, 0, 10, 10);

        Assert.False(a.Intersects(new Rect2(10, 0, 20, 10)));
        Assert.True(a.Intersects(new Rect2(9.9f, 0, 20, 10)));
    }

    [Fact]
    public void ExpandGrowsEverySide()
    {
        var rect = new Rect2(0, 0, 10, 10).Expand(2);

        Assert.Equal(-2, rect.MinX, 5);
        Assert.Equal(12, rect.MaxX, 5);
        Assert.Equal(14, rect.Width, 5);
    }

    [Fact]
    public void ASegmentThroughTheRectangleIntersects()
    {
        var rect = new Rect2(0, 0, 10, 10);

        Assert.True(rect.IntersectsSegment(new Vec2(-5, 5), new Vec2(15, 5)));
        Assert.True(rect.IntersectsSegment(new Vec2(5, -5), new Vec2(5, 15)));
        Assert.True(rect.IntersectsSegment(new Vec2(-5, -5), new Vec2(15, 15)));
    }

    [Fact]
    public void ASegmentBesideTheRectangleDoesNotIntersect()
    {
        var rect = new Rect2(0, 0, 10, 10);

        Assert.False(rect.IntersectsSegment(new Vec2(-5, 20), new Vec2(15, 20)));
        Assert.False(rect.IntersectsSegment(new Vec2(-5, -5), new Vec2(-1, 15)));
    }

    [Fact]
    public void ASegmentThatStopsBeforeTheRectangleDoesNotIntersect()
    {
        var rect = new Rect2(0, 0, 10, 10);

        Assert.False(rect.IntersectsSegment(new Vec2(-5, 5), new Vec2(-1, 5)));
    }
}
