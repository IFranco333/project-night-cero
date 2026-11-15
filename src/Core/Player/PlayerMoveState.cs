using NightCero.Core.Math;

namespace NightCero.Core.Player;

/// <summary>
/// What the player wants this tick: a horizontal direction (already in world space) and whether
/// they are sprinting. The Godot layer turns input into <see cref="MoveIntent"/>s; this component
/// is the simulation's copy of the latest intent.
/// </summary>
public readonly record struct PlayerMoveState(Vec2 MoveDirection, bool Sprinting);
