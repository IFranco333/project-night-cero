using NightCero.Core.Rng;

namespace NightCero.Tests.Core;

public sealed class RngStreamTests
{
    [Fact]
    public void SameSeedAndStreamProduceTheSameSequence()
    {
        var a = new RngStream(seed: 1234, name: "loot");
        var b = new RngStream(seed: 1234, name: "loot");

        for (var i = 0; i < 1_000; i++)
        {
            Assert.Equal(a.NextULong(), b.NextULong());
        }
    }

    [Fact]
    public void DifferentSeedsProduceDifferentSequences()
    {
        var a = new RngStream(seed: 1234, name: "loot");
        var b = new RngStream(seed: 1235, name: "loot");

        var aValues = Enumerable.Range(0, 16).Select(_ => a.NextULong()).ToList();
        var bValues = Enumerable.Range(0, 16).Select(_ => b.NextULong()).ToList();

        Assert.NotEqual(aValues, bValues);
    }

    [Fact]
    public void DifferentStreamNamesProduceDifferentSequences()
    {
        var loot = new RngStream(seed: 1234, name: "loot");
        var zombie = new RngStream(seed: 1234, name: "zombie-ai");

        Assert.NotEqual(loot.NextULong(), zombie.NextULong());
    }

    [Fact]
    public void ForkDependsOnTheParentStreamNotOnlyOnTheName()
    {
        var world = new RngStream(seed: 1234, name: "world");
        var other = new RngStream(seed: 9999, name: "world");

        Assert.NotEqual(world.Fork("loot").NextULong(), other.Fork("loot").NextULong());
    }

    [Fact]
    public void ForkingDoesNotAdvanceTheParentStream()
    {
        var parent = new RngStream(seed: 1234, name: "world");
        var expected = new RngStream(seed: 1234, name: "world").NextULong();

        parent.Fork("loot");
        parent.Fork("loot");
        parent.Fork("combat");

        Assert.Equal(expected, parent.NextULong());
    }

    [Fact]
    public void ForkIsStableWhenSiblingsAreConsumedInAnyOrder()
    {
        // System A: fork both streams, then draw "loot" first and consume "zombie-ai" afterwards.
        var a = new RngStream(seed: 42, name: "world");
        var lootInA = a.Fork("loot");
        var aiInA = a.Fork("zombie-ai");
        var lootValue = lootInA.NextULong();
        aiInA.NextULong();
        aiInA.NextULong();

        // System B: consume "zombie-ai" first, then fork "loot" and draw from it.
        var b = new RngStream(seed: 42, name: "world");
        var aiInB = b.Fork("zombie-ai");
        aiInB.NextULong();
        aiInB.NextULong();
        var lootOther = b.Fork("loot").NextULong();

        Assert.Equal(lootValue, lootOther);
    }

    [Fact]
    public void IntInRangeIsInclusiveAndCoversBothEnds()
    {
        var rng = new RngStream(seed: 7, name: "test");
        var seen = new HashSet<int>();

        for (var i = 0; i < 10_000; i++)
        {
            var value = rng.NextInt(1, 3);
            Assert.InRange(value, 1, 3);
            seen.Add(value);
        }

        Assert.Equal(new[] { 1, 2, 3 }, seen.OrderBy(v => v).ToArray());
    }

    [Fact]
    public void NextIntWithEmptyRangeReturnsTheOnlyValue()
    {
        var rng = new RngStream(seed: 7, name: "test");

        Assert.Equal(5, rng.NextInt(5, 5));
    }

    [Fact]
    public void NextIntRejectsAnInvertedRange()
    {
        var rng = new RngStream(seed: 7, name: "test");

        Assert.Throws<ArgumentException>(() => rng.NextInt(5, 4));
    }

    [Fact]
    public void DoubleIsInTheUnitInterval()
    {
        var rng = new RngStream(seed: 7, name: "test");

        for (var i = 0; i < 10_000; i++)
        {
            var value = rng.NextDouble();
            Assert.True(value >= 0.0 && value < 1.0, $"out of range: {value}");
        }
    }

    [Fact]
    public void ShuffleIsAPermutationDrivenByTheStream()
    {
        var items = Enumerable.Range(0, 100).ToArray();

        var a = new RngStream(seed: 7, name: "shuffle");
        var b = new RngStream(seed: 7, name: "shuffle");
        var first = items.ToArray();
        var second = items.ToArray();
        a.Shuffle(first);
        b.Shuffle(second);

        Assert.Equal(first, second);
        Assert.Equal(items, first.OrderBy(v => v).ToArray());
    }

    [Fact]
    public void EmptyStreamNameIsRejected()
    {
        Assert.Throws<ArgumentException>(() => new RngStream(seed: 1, name: ""));
        Assert.Throws<ArgumentException>(() => new RngStream(seed: 1, name: " "));
    }

    /// <summary>
    /// Pins the generator itself. If this test changes, every replay and every golden world hash
    /// changes with it: bump the save version and write a migration (ADR 0002).
    /// </summary>
    [Fact]
    public void GoldenSequenceIsStableForSeed2026()
    {
        var rng = new RngStream(seed: 2026, name: "golden");

        var values = Enumerable.Range(0, 5).Select(_ => rng.NextULong()).ToArray();

        Assert.Equal(
            new ulong[]
            {
                2181359868267673246UL,
                17549052430653723447UL,
                13169526690237934515UL,
                653587118166948916UL,
                12610385910013618485UL,
            },
            values);
    }
}
