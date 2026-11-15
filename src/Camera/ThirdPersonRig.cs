using NightCero.Core.Math;

namespace NightCero.Camera;

/// <summary>Where the camera goes and what it looks at.</summary>
public readonly record struct CameraPose(Vec3 Position, Vec3 LookAt);

/// <summary>
/// Pure math of the third-person camera: no Godot types, so it is unit-tested like the simulation.
/// </summary>
/// <remarks>
/// Angles use the engine convention: yaw 0 faces north (-Z) and grows clockwise seen from above;
/// pitch 0 looks level and positive pitch looks down from above the target.
/// </remarks>
public static class ThirdPersonRig
{
    /// <summary>Height of the point the camera looks at, above the target's feet.</summary>
    public const float HeadHeight = 1.6f;

    /// <summary>Lowest allowed pitch (looking up at the sky).</summary>
    public const float MinPitchDegrees = -30f;

    /// <summary>Highest allowed pitch (looking down at the ground).</summary>
    public const float MaxPitchDegrees = 70f;

    /// <summary>Computes the camera pose for a target and orbit angles.</summary>
    /// <param name="target">The followed point (the player's feet).</param>
    /// <param name="yawDegrees">Horizontal angle around the target.</param>
    /// <param name="pitchDegrees">Vertical angle; clamped to [<see cref="MinPitchDegrees"/>, <see cref="MaxPitchDegrees"/>].</param>
    /// <param name="distance">Orbit radius in metres, measured from the head; clamped to zero.</param>
    public static CameraPose Evaluate(
        Vec3 target,
        float yawDegrees,
        float pitchDegrees,
        float distance)
    {
        var yaw = yawDegrees * (MathF.PI / 180f);
        var pitch = Clamp(pitchDegrees, MinPitchDegrees, MaxPitchDegrees) * (MathF.PI / 180f);
        var radius = MathF.Max(distance, 0f);
        var lookAt = target + (Vec3.Up * HeadHeight);

        // Behind the target: opposite the facing direction, lifted by pitch. Yaw grows clockwise
        // seen from above, so the camera slides the other way horizontally (hence the negated X).
        var offset = new Vec3(
            -MathF.Sin(yaw) * MathF.Cos(pitch) * radius,
            MathF.Sin(pitch) * radius,
            MathF.Cos(yaw) * MathF.Cos(pitch) * radius);

        // The orbit is centred on the looked-at head point, so pitch never changes the distance.
        return new CameraPose(lookAt + offset, lookAt);
    }

    private static float Clamp(float value, float min, float max) => MathF.Min(MathF.Max(value, min), max);
}
