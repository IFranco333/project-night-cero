using NightCero.Core.Math;

namespace NightCero.Core.Player;

/// <summary>
/// An intent sent from the presentation layer to the simulation: "move in this world-space
/// direction" plus "sprint or not". The simulation decides how far that moves the player.
/// </summary>
public sealed record MoveIntent(Vec2 Direction, bool Sprint = false)
{
    /// <summary>Walking speed in metres per second. Mirrored in <c>docs/design.md</c>.</summary>
    public const float WalkSpeedMetersPerSecond = 3.5f;

    /// <summary>Sprinting speed in metres per second. Mirrored in <c>docs/design.md</c>.</summary>
    public const float SprintSpeedMetersPerSecond = 6.0f;

    /// <summary>How much faster sprinting is than walking, derived so the two speeds stay in sync.</summary>
    public const float SprintMultiplier = SprintSpeedMetersPerSecond / WalkSpeedMetersPerSecond;
}
