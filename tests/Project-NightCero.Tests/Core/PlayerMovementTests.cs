using NightCero.Core.Math;
using NightCero.Core.Player;
using NightCero.Core.Simulation;
using NightCero.Tests.Support;

namespace NightCero.Tests.Core;

public sealed class PlayerMovementTests
{
    private const float TickSeconds = 0.1f;

    [Fact]
    public void ForwardIntentMovesNorthAtWalkSpeed()
    {
        var simulation = PlayerScene.Create();

        simulation.Submit(new MoveIntent(Vec2.Forward));
        simulation.Run(1);

        var player = simulation.PlayerView();
        Assert.Equal(0, player.Transform.Position.X, 4);
        Assert.Equal(-MoveIntent.WalkSpeedMetersPerSecond * TickSeconds, player.Transform.Position.Z, 4);
    }

    [Fact]
    public void BackwardIntentMovesSouth()
    {
        var simulation = PlayerScene.Create();

        simulation.Submit(new MoveIntent(Vec2.Forward * -1));
        simulation.Run(1);

        Assert.True(simulation.PlayerView().Transform.Position.Z > 0);
    }

    [Fact]
    public void RightIntentMovesEastIndependentlyOfFacing()
    {
        var simulation = PlayerScene.Create();

        simulation.Submit(new MoveIntent(Vec2.Right));
        simulation.Run(1);

        Assert.True(simulation.PlayerView().Transform.Position.X > 0);
        Assert.Equal(0, simulation.PlayerView().Transform.Position.Z, 4);
    }

    [Fact]
    public void SprintingIsFasterThanWalkingByTheSprintMultiplier()
    {
        var walk = PlayerScene.Create();
        walk.Submit(new MoveIntent(Vec2.Forward));
        walk.Run(5);

        var sprint = PlayerScene.Create();
        sprint.Submit(new MoveIntent(Vec2.Forward, Sprint: true));
        sprint.Run(5);

        var walked = MathF.Abs(walk.PlayerView().Transform.Position.Z);
        var sprinted = MathF.Abs(sprint.PlayerView().Transform.Position.Z);
        Assert.True(sprinted > walked);
        Assert.Equal(walked * MoveIntent.SprintMultiplier, sprinted, 3);
    }

    [Fact]
    public void DiagonalInputIsNormalizedSoItIsNotFaster()
    {
        var diagonal = PlayerScene.Create();
        diagonal.Submit(new MoveIntent(new Vec2(1, 1)));
        diagonal.Run(10);

        var straight = PlayerScene.Create();
        straight.Submit(new MoveIntent(Vec2.Forward));
        straight.Run(10);

        Assert.Equal(
            MathF.Abs(straight.PlayerView().Transform.Position.Z),
            diagonal.PlayerView().Transform.Position.LengthXZ,
            3);
    }

    [Fact]
    public void NoIntentLeavesThePlayerStill()
    {
        var simulation = PlayerScene.Create();
        var before = simulation.PlayerView().Transform.Position;

        simulation.Run(20);

        Assert.Equal(before, simulation.PlayerView().Transform.Position);
    }

    [Fact]
    public void TheSameIntentsProduceTheSameFinalHash()
    {
        ulong Play()
        {
            var simulation = PlayerScene.Create(seed: 77);
            simulation.Submit(new MoveIntent(new Vec2(1, 0.3f), Sprint: true));
            simulation.Run(7);
            simulation.Submit(new MoveIntent(new Vec2(-1, 0)));
            simulation.Run(3);
            return simulation.StateHash();
        }

        Assert.Equal(Play(), Play());
    }

    [Fact]
    public void FacingFollowsTheMovementDirection()
    {
        var north = PlayerScene.Create();
        north.Submit(new MoveIntent(Vec2.Forward));
        north.Run(1);

        var east = PlayerScene.Create();
        east.Submit(new MoveIntent(Vec2.Right));
        east.Run(1);

        Assert.Equal(0f, north.PlayerView().Transform.YawDegrees, 4);
        Assert.Equal(90f, east.PlayerView().Transform.YawDegrees, 4);
    }

    [Fact]
    public void EntitiesWithoutATransformAreIgnored()
    {
        var simulation = new Simulation(seed: 1);
        simulation.World.Spawn(new PlayerMoveState(Vec2.Forward, Sprinting: false));
        simulation.World.AddSystem(new PlayerMovementSystem());

        simulation.Run(1);

        Assert.Equal(1, simulation.Tick);
    }

    [Fact]
    public void SubmittingAnIntentReplacesThePreviousOne()
    {
        var simulation = PlayerScene.Create();
        simulation.Submit(new MoveIntent(Vec2.Right));
        simulation.Run(1);

        simulation.Submit(new MoveIntent(Vec2.Forward));
        simulation.Run(1);

        Assert.Equal(Vec2.Forward, simulation.PlayerView().Movement.MoveDirection);
    }
}
