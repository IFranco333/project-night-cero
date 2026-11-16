using Godot;

namespace NightCero.Game.Rendering;

/// <summary>
/// Every colour the renderer uses, in one place.
/// </summary>
/// <remarks>
/// <para>
/// Art direction is a living document, not a one-time decision: the whole look of the town can be
/// retuned by editing this file, because nothing else hard-codes a colour. Keeping the palette
/// separate also means the lighting presets in <see cref="Mood"/> and the materials here can be
/// tuned independently — see <c>docs/art-direction.md</c>.
/// </para>
/// <para>
/// The ranges are deliberately narrow and desaturated. The reference frame is a grey-green,
/// fog-bound night: surfaces sit close together in value, and the only saturated colour is the
/// accent, which is what makes it read as light rather than paint.
/// </para>
/// </remarks>
public static class Palette
{
    // ---- Ground --------------------------------------------------------------------------

    public static readonly Color Ground = new(0.075f, 0.080f, 0.085f);
    public static readonly Color Road = new(0.145f, 0.145f, 0.160f);
    public static readonly Color RoadMarking = new(0.42f, 0.40f, 0.33f);
    public static readonly Color Sidewalk = new(0.265f, 0.265f, 0.270f);
    public static readonly Color Curb = new(0.32f, 0.32f, 0.32f);
    public static readonly Color Grass = new(0.095f, 0.165f, 0.095f);
    public static readonly Color ForestFloor = new(0.070f, 0.105f, 0.065f);
    public static readonly Color Dirt = new(0.155f, 0.125f, 0.095f);
    public static readonly Color ParkPath = new(0.295f, 0.275f, 0.245f);

    // ---- Buildings -----------------------------------------------------------------------

    public static readonly Color House = new(0.335f, 0.295f, 0.255f);
    public static readonly Color Mansion = new(0.300f, 0.270f, 0.250f);
    public static readonly Color Shop = new(0.295f, 0.305f, 0.335f);
    public static readonly Color Warehouse = new(0.255f, 0.265f, 0.265f);
    public static readonly Color Garage = new(0.225f, 0.235f, 0.245f);
    public static readonly Color Church = new(0.375f, 0.355f, 0.315f);
    public static readonly Color Hospital = new(0.430f, 0.440f, 0.460f);
    public static readonly Color Police = new(0.235f, 0.255f, 0.300f);
    public static readonly Color Unknown = new(0.300f, 0.300f, 0.300f);
    public static readonly Color Door = new(0.032f, 0.032f, 0.038f);
    public static readonly Color Window = new(0.055f, 0.060f, 0.075f);

    // ---- Props ---------------------------------------------------------------------------

    public static readonly Color Trunk = new(0.125f, 0.100f, 0.075f);
    public static readonly Color Conifer = new(0.075f, 0.135f, 0.090f);
    public static readonly Color Canopy = new(0.105f, 0.175f, 0.115f);
    public static readonly Color Bench = new(0.250f, 0.195f, 0.140f);
    public static readonly Color Dumpster = new(0.135f, 0.200f, 0.165f);
    public static readonly Color Pole = new(0.190f, 0.190f, 0.200f);

    /// <summary>Colours of the parked cars; a muted, second-hand palette.</summary>
    public static readonly Color[] Cars =
    [
        new Color(0.155f, 0.165f, 0.185f),
        new Color(0.195f, 0.115f, 0.095f),
        new Color(0.115f, 0.135f, 0.175f),
        new Color(0.185f, 0.185f, 0.165f),
    ];

    // ---- Light ---------------------------------------------------------------------------

    /// <summary>
    /// The accent: cold violet-blue. It marks the light the player cares about — streetlights,
    /// windows lit from inside, the glow under a cellar door — and is the only chromatic note in
    /// the scene. Night-things get this colour instead of the usual sickly green.
    /// </summary>
    public static readonly Color Accent = new(0.48f, 0.52f, 0.95f);

    /// <summary>A lighter accent for text and glyphs, where the display colour would read as black.</summary>
    public static readonly Color AccentBright = new(0.72f, 0.76f, 1.0f);

    /// <summary>A dimmer, redder relative of the accent, for warning light (brake lights, beacons).</summary>
    public static readonly Color Warning = new(0.85f, 0.42f, 0.30f);

    /// <summary>Warm interior light: candles and lamps behind a window.</summary>
    public static readonly Color Interior = new(1.0f, 0.72f, 0.42f);

    /// <summary>The colour of a lit window pane: interior warmth tinted by the accent.</summary>
    public static readonly Color LitWindow = new(0.72f, 0.66f, 0.62f);
}
