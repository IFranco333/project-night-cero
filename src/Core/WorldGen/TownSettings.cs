namespace NightCero.Core.WorldGen;

/// <summary>
/// The shape of a generated town: a rectangle of blocks separated by a road grid.
/// </summary>
/// <remarks>
/// <see cref="Width"/> and <see cref="Height"/> must be exact multiples of <see cref="BlockPitch"/>,
/// so every block is the same size and the road grid closes on itself. The defaults describe the
/// ~1 km² town of the vertical slice.
/// </remarks>
public readonly record struct TownSettings(
    int Width = 1024,
    int Height = 1024,
    int BlockPitch = 64,
    int RoadWidth = 8,
    int BuildingMargin = 2)
{
    /// <summary>The settings of the vertical-slice town.</summary>
    /// <remarks>
    /// Written out explicitly: <c>new()</c> on a struct zero-initialises and would ignore the
    /// optional parameter values.
    /// </remarks>
    public static TownSettings Default => new(Width: 1024, Height: 1024, BlockPitch: 64, RoadWidth: 8, BuildingMargin: 2);

    /// <summary>Number of block columns.</summary>
    public int BlocksX => Width / BlockPitch;

    /// <summary>Number of block rows.</summary>
    public int BlocksZ => Height / BlockPitch;

    /// <summary>Throws when the settings cannot produce a valid town.</summary>
    public void Validate()
    {
        if (Width <= 0 || Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Width), "The town needs a positive size.");
        }

        if (BlockPitch < 16)
        {
            throw new ArgumentOutOfRangeException(nameof(BlockPitch), "A block pitch below 16 m leaves no room for a building.");
        }

        if (Width % BlockPitch != 0 || Height % BlockPitch != 0)
        {
            throw new ArgumentException("Width and height must be exact multiples of the block pitch.", nameof(Width));
        }

        if (RoadWidth < 4 || RoadWidth * 2 >= BlockPitch)
        {
            throw new ArgumentOutOfRangeException(nameof(RoadWidth), "The road must be at least 4 m and narrower than half a block.");
        }

        if (BuildingMargin < 0 || (BuildingMargin * 2) >= BlockPitch - RoadWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(BuildingMargin), "The margin leaves no buildable area inside a block.");
        }
    }
}
