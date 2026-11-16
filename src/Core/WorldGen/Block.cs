using NightCero.Core.Math;

namespace NightCero.Core.WorldGen;

/// <summary>What a town block holds. Decided once per block by the generator.</summary>
public enum BlockKind
{
    /// <summary>Built up: up to four buildings and a sidewalk around them.</summary>
    Built,

    /// <summary>A park: grass, trees and a path across it.</summary>
    Park,

    /// <summary>A wood: dense trees, no buildings and no path.</summary>
    Forest,

    /// <summary>An empty lot: bare ground, no buildings.</summary>
    Empty,
}

/// <summary>
/// One block of the town grid: the buildable square between the roads, and what it holds.
/// </summary>
/// <remarks>
/// The bounds are the block inset by half a road on every side, so the band between the bounds and
/// the buildings inside them is the sidewalk. The renderer draws that band as a raised slab; the
/// generator uses the same rectangle to place buildings, which is why the two can never disagree.
/// </remarks>
public readonly record struct Block(Rect2 Bounds, BlockKind Kind);
