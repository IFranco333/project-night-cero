using NightCero.Runner;

namespace NightCero.Tests.Core;

public sealed class RunnerOptionsTests
{
    [Fact]
    public void DefaultsAreStable()
    {
        var options = RunnerOptions.Parse([]);

        Assert.NotNull(options);
        Assert.Equal(1UL, options.Seed);
        Assert.Equal(100, options.Ticks);
        Assert.Equal(4, options.Bodies);
        Assert.False(options.ShowState);
        Assert.False(options.Bench);
    }

    [Fact]
    public void AllOptionsAreParsed()
    {
        var options = RunnerOptions.Parse(["--seed", "2026", "--ticks", "250", "--bodies", "8", "--state", "--bench"]);

        Assert.Equal(new RunnerOptions(2026, 250, 8, true, true), options);
    }

    [Fact]
    public void BenchFlagAloneIsParsed()
    {
        var options = RunnerOptions.Parse(["--bench"]);

        Assert.NotNull(options);
        Assert.True(options.Bench);
        Assert.False(options.ShowState);
    }

    [Theory]
    [InlineData("--ticks")]
    [InlineData("--ticks -1")]
    [InlineData("--seed abc")]
    [InlineData("--unknown")]
    public void InvalidArgumentsReturnNull(string argumentLine)
    {
        Assert.Null(RunnerOptions.Parse(argumentLine.Split(' ')));
    }
}
