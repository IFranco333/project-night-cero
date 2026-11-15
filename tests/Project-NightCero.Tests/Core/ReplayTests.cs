using NightCero.Core.Math;
using NightCero.Core.Player;
using NightCero.Core.Simulation;
using NightCero.Tests.Support;

namespace NightCero.Tests.Core;

public sealed class ReplayTests
{
    private static readonly MoveIntent?[] Script =
    [
        new MoveIntent(Vec2.Forward),
        new MoveIntent(Vec2.Forward, Sprint: true),
        null,
        new MoveIntent(new Vec2(1, 0.5f)),
        new MoveIntent(Vec2.Right),
        null,
        new MoveIntent(new Vec2(-1, -1), Sprint: true),
    ];

    [Fact]
    public void ReplayingARecordedLogReproducesTheSession()
    {
        var log = new InputLog();
        var live = PlayerScene.Create(seed: 4242);
        foreach (var intent in Script)
        {
            if (intent is not null)
            {
                live.Submit(intent);
            }

            live.Run(1);
            log.Add(intent);
        }

        var replay = PlayerScene.Create(seed: 4242);
        replay.Run(log);

        Assert.Equal(live.Tick, replay.Tick);
        Assert.Equal(live.StateHash(), replay.StateHash());
        Assert.Equal(live.Checkpoint().State, replay.Checkpoint().State);
    }

    [Fact]
    public void TheSameLogOnADifferentSeedDiverges()
    {
        var log = new InputLog();
        foreach (var intent in Script)
        {
            log.Add(intent);
        }

        var a = PlayerScene.Create(seed: 1);
        var b = PlayerScene.Create(seed: 2);
        a.Run(log);
        b.Run(log);

        Assert.NotEqual(a.StateHash(), b.StateHash());
    }

    [Fact]
    public void ADifferentLogDiverges()
    {
        var forward = new InputLog();
        var right = new InputLog();
        for (var i = 0; i < 5; i++)
        {
            forward.Add(new MoveIntent(Vec2.Forward));
            right.Add(new MoveIntent(Vec2.Right));
        }

        var a = PlayerScene.Create(seed: 9);
        var b = PlayerScene.Create(seed: 9);
        a.Run(forward);
        b.Run(right);

        Assert.NotEqual(a.StateHash(), b.StateHash());
    }

    [Fact]
    public void AnEmptyLogRunsNoTicks()
    {
        var simulation = PlayerScene.Create();

        simulation.Run(new InputLog());

        Assert.Equal(0, simulation.Tick);
    }

    [Fact]
    public void RunRejectsANullLog()
    {
        var simulation = PlayerScene.Create();

        Assert.Throws<ArgumentNullException>(() => simulation.Run((InputLog)null!));
    }
}
