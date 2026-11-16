using Godot;

namespace NightCero.Game.Rendering;

/// <summary>
/// A lighting preset: sky, ambient, fog and the key light. The renderer draws the same town under
/// any of these, which is how the same layout can be judged at dusk and at midnight.
/// </summary>
/// <param name="Name">Short name, used on the command line and in the debug line.</param>
/// <param name="Ambient">Flat ambient fill; the floor below which shadows never go.</param>
/// <param name="AmbientEnergy">How strongly the ambient colour tints the scene.</param>
/// <param name="Fog">Colour of the distance fog.</param>
/// <param name="FogDensity">Fog density; higher pulls the horizon closer.</param>
/// <param name="Sky">The sky drawn behind the town.</param>
/// <param name="KeyColor">Colour of the directional key light.</param>
/// <param name="KeyEnergy">Strength of the key light.</param>
/// <param name="MoonDirection">
/// Direction from the ground to the moon. The key light points the opposite way, so the moon and
/// the shadows always agree; there is no second number to keep in sync.
/// </param>
public sealed record Mood(
    string Name,
    Color Ambient,
    float AmbientEnergy,
    Color Fog,
    float FogDensity,
    SkyLook Sky,
    Color KeyColor,
    float KeyEnergy,
    Vector3 MoonDirection)
{
    /// <summary>
    /// The default: a clear-to-broken night with a moon. Almost monochrome, and the fog keeps the
    /// far edge of town from reading as a wall of boxes.
    /// </summary>
    public static Mood Night => new(
        "night",
        Ambient: new Color(0.240f, 0.280f, 0.360f),
        AmbientEnergy: 1.15f,
        Fog: new Color(0.070f, 0.080f, 0.120f),
        FogDensity: 0.0022f,
        Sky: SkyLook.ClearNight,
        KeyColor: new Color(0.550f, 0.650f, 0.900f),
        KeyEnergy: 0.85f,
        MoonDirection: new Vector3(-0.45f, 0.55f, -0.70f));

    /// <summary>A grey, windless day: the reference frame's flat overcast light, no colour.</summary>
    public static Mood Overcast => new(
        "overcast",
        Ambient: new Color(0.55f, 0.57f, 0.60f),
        AmbientEnergy: 0.95f,
        Fog: new Color(0.48f, 0.50f, 0.53f),
        FogDensity: 0.0018f,
        Sky: SkyLook.Overcast,
        KeyColor: new Color(0.82f, 0.84f, 0.88f),
        KeyEnergy: 0.55f,
        MoonDirection: new Vector3(0.2f, 0.8f, 0.4f));

    /// <summary>Late dusk: the last warm light, long shadows, the town already unreadable.</summary>
    public static Mood Dusk => new(
        "dusk",
        Ambient: new Color(0.30f, 0.24f, 0.26f),
        AmbientEnergy: 0.95f,
        Fog: new Color(0.18f, 0.12f, 0.13f),
        FogDensity: 0.0026f,
        Sky: SkyLook.Dusk,
        KeyColor: new Color(0.95f, 0.55f, 0.38f),
        KeyEnergy: 0.75f,
        MoonDirection: new Vector3(0.6f, 0.12f, 0.7f));

    /// <summary>Every preset, in the order <c>--mood</c> cycles them.</summary>
    public static IReadOnlyList<Mood> All { get; } = [Night, Overcast, Dusk];

    /// <summary>Finds a preset by name, case-insensitively; null when there is none.</summary>
    public static Mood? For(string name)
    {
        foreach (var mood in All)
        {
            if (string.Equals(mood.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return mood;
            }
        }

        return null;
    }

    /// <summary>The preset after <paramref name="name"/> in <see cref="All"/>, wrapping around.</summary>
    public static Mood Next(string name)
    {
        for (var i = 0; i < All.Count; i++)
        {
            if (string.Equals(All[i].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return All[(i + 1) % All.Count];
            }
        }

        return All[0];
    }

    /// <summary>Builds the world environment for this preset.</summary>
    public Godot.Environment BuildEnvironment() => new()
    {
        BackgroundMode = Godot.Environment.BGMode.Sky,
        Sky = Sky.Build(),

        // Colour ambient rather than sky ambient: the sky shader already carries the stars and the
        // moon, and its radiance is too bright for the shadow floor we want.
        AmbientLightSource = Godot.Environment.AmbientSource.Color,
        AmbientLightColor = Ambient,
        AmbientLightEnergy = AmbientEnergy,
        TonemapMode = Godot.Environment.ToneMapper.Filmic,
        FogEnabled = true,
        FogLightColor = Fog,
        FogDensity = FogDensity,
    };

    /// <summary>Builds the directional key light for this preset.</summary>
    public DirectionalLight3D BuildKeyLight(bool shadows) => new()
    {
        Name = "KeyLight",
        LightColor = KeyColor,
        LightEnergy = KeyEnergy,
        ShadowEnabled = shadows,
        Basis = LightBasis(),
    };

    /// <summary>
    /// The light frame that shines from the moon down at the town. A directional light emits along
    /// its local −Z, so the basis looks at the ground (the opposite of the moon direction).
    /// </summary>
    private Basis LightBasis()
    {
        var toMoon = MoonDirection.Normalized();

        // Straight-overhead light has no usable up vector; tilt it so the look-at stays well defined.
        var up = MathF.Abs(toMoon.Y) > 0.98f ? Vector3.Forward : Vector3.Up;
        return Basis.LookingAt(-toMoon, up);
    }
}
