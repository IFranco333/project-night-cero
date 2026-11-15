using NightCero.Core.Math;
using NightCero.Camera;

namespace NightCero.Tests.Game;

public sealed class CameraRigTests
{
    [Fact]
    public void DefaultRigSitsBehindTheTarget()
    {
        var camera = ThirdPersonRig.Evaluate(Vec3.Zero, yawDegrees: 0, pitchDegrees: 0, distance: 3);

        // Yaw 0 faces north (-Z), so the camera is south of the target (+Z), level with the head.
        Assert.Equal(0, camera.Position.X, 4);
        Assert.Equal(ThirdPersonRig.HeadHeight, camera.Position.Y, 4);
        Assert.Equal(3f, camera.Position.Z, 4);
    }

    [Fact]
    public void RotatingYawMovesTheCameraAroundTheTarget()
    {
        var east = ThirdPersonRig.Evaluate(Vec3.Zero, yawDegrees: 90, pitchDegrees: 0, distance: 3);
        var south = ThirdPersonRig.Evaluate(Vec3.Zero, yawDegrees: 180, pitchDegrees: 0, distance: 3);

        // Facing east (+X) puts the camera west (-X); facing south (+Z) puts it north (-Z).
        Assert.Equal(-3f, east.Position.X, 4);
        Assert.Equal(0, east.Position.Z, 4);
        Assert.Equal(0, south.Position.X, 4);
        Assert.Equal(-3f, south.Position.Z, 4);
    }

    [Fact]
    public void CameraLooksAtTheTargetHead()
    {
        var target = new Vec3(10, 0, -4);

        var camera = ThirdPersonRig.Evaluate(target, yawDegrees: 45, pitchDegrees: 0, distance: 4);

        Assert.Equal(target.X, camera.LookAt.X, 4);
        Assert.Equal(target.Y + ThirdPersonRig.HeadHeight, camera.LookAt.Y, 4);
        Assert.Equal(target.Z, camera.LookAt.Z, 4);
    }

    [Fact]
    public void PitchRaisesTheCameraAndKeepsTheDistance()
    {
        var flat = ThirdPersonRig.Evaluate(Vec3.Zero, yawDegrees: 0, pitchDegrees: 0, distance: 5);
        var pitched = ThirdPersonRig.Evaluate(Vec3.Zero, yawDegrees: 0, pitchDegrees: 30, distance: 5);

        Assert.True(pitched.Position.Y > flat.Position.Y);
        Assert.Equal(5f, (pitched.Position - pitched.LookAt).Length, 3);
    }

    [Fact]
    public void PitchIsClampedToTheAllowedRange()
    {
        var tooHigh = ThirdPersonRig.Evaluate(Vec3.Zero, yawDegrees: 0, pitchDegrees: 500, distance: 5);
        var clamped = ThirdPersonRig.Evaluate(
            Vec3.Zero, yawDegrees: 0, pitchDegrees: ThirdPersonRig.MaxPitchDegrees, distance: 5);

        Assert.Equal(clamped.Position, tooHigh.Position);
    }

    [Fact]
    public void NonPositiveDistancePutsTheCameraOnTheTarget()
    {
        var camera = ThirdPersonRig.Evaluate(Vec3.Zero, yawDegrees: 0, pitchDegrees: 0, distance: -3);
        var lookAt = new Vec3(0, ThirdPersonRig.HeadHeight, 0);

        Assert.Equal(lookAt.X, camera.Position.X, 4);
        Assert.Equal(lookAt.Z, camera.Position.Z, 4);
    }
}
