using NightCero.Core.Math;

namespace NightCero.Core.WorldModel;

/// <summary>A world-space pose. Yaw is the horizontal facing in degrees, clockwise from north.</summary>
public readonly record struct Transform(Vec3 Position, float YawDegrees);
