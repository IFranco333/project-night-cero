using Godot;

namespace NightCero.Game.Rendering;

/// <summary>
/// The sky of a <see cref="Mood"/>: a procedural gradient with drifting cloud, stars and a moon.
/// </summary>
/// <remarks>
/// <para>
/// It is a sky shader rather than a texture because the sky has to be free: there are no sky
/// images in the project, and a shader gives clouds and stars at any resolution with no download.
/// It works on the Compatibility backend, which the renderer defaults to.
/// </para>
/// <para>
/// Every field maps to a uniform in <c>Rendering/NightSky.gdshader</c>. The moon's direction follows
/// the key light, so the brightest thing in the sky and the shadow direction agree.
/// </para>
/// </remarks>
/// <param name="Top">Colour straight up.</param>
/// <param name="Horizon">Colour at the horizon, where fog takes over.</param>
/// <param name="MoonDirection">Direction from the camera to the moon, in world space.</param>
/// <param name="MoonColor">Colour of the moon disc and its halo.</param>
/// <param name="MoonSize">Angular radius of the disc.</param>
/// <param name="MoonGlow">Strength of the halo around the disc.</param>
/// <param name="StarDensity">Threshold a hashed cell must pass to hold a star; higher is sparser.</param>
/// <param name="StarBrightness">Brightness of the stars.</param>
/// <param name="CloudCoverage">Cloud fraction: 0 is clear, 1 is solid overcast.</param>
/// <param name="CloudColor">Colour of the cloud deck.</param>
/// <param name="CloudSpeed">Drift speed of the cloud deck.</param>
public sealed record SkyLook(
    Color Top,
    Color Horizon,
    Vector3 MoonDirection,
    Color MoonColor,
    float MoonSize,
    float MoonGlow,
    float StarDensity,
    float StarBrightness,
    float CloudCoverage,
    Color CloudColor,
    float CloudSpeed)
{
    /// <summary>Path of the shader, relative to the project root.</summary>
    public const string ShaderPath = "res://Rendering/NightSky.gdshader";

    /// <summary>Clear night: dense stars, a bright moon, thin high cloud.</summary>
    public static SkyLook ClearNight => new(
        Top: new Color(0.010f, 0.014f, 0.030f),
        Horizon: new Color(0.045f, 0.052f, 0.085f),
        MoonDirection: new Vector3(-0.45f, 0.55f, -0.70f),
        MoonColor: new Color(0.88f, 0.92f, 1.0f),
        MoonSize: 0.022f,
        MoonGlow: 0.55f,
        StarDensity: 0.9965f,
        StarBrightness: 1.0f,
        CloudCoverage: 0.42f,
        CloudColor: new Color(0.075f, 0.085f, 0.115f),
        CloudSpeed: 0.004f);

    /// <summary>Overcast day: the cloud deck is solid, no stars, no sun disc.</summary>
    public static SkyLook Overcast => new(
        Top: new Color(0.36f, 0.38f, 0.40f),
        Horizon: new Color(0.50f, 0.52f, 0.54f),
        MoonDirection: new Vector3(0.2f, 0.8f, 0.4f),
        MoonColor: new Color(0.8f, 0.8f, 0.8f),
        MoonSize: 0.0f,
        MoonGlow: 0.0f,
        StarDensity: 2.0f,
        StarBrightness: 0.0f,
        CloudCoverage: 0.15f,
        CloudColor: new Color(0.42f, 0.44f, 0.47f),
        CloudSpeed: 0.006f);

    /// <summary>Dusk: a warm horizon under broken cloud; the first stars appear.</summary>
    public static SkyLook Dusk => new(
        Top: new Color(0.055f, 0.045f, 0.085f),
        Horizon: new Color(0.34f, 0.19f, 0.15f),
        MoonDirection: new Vector3(0.6f, 0.12f, 0.7f),
        MoonColor: new Color(1.0f, 0.82f, 0.66f),
        MoonSize: 0.018f,
        MoonGlow: 0.7f,
        StarDensity: 0.9990f,
        StarBrightness: 0.6f,
        CloudCoverage: 0.50f,
        CloudColor: new Color(0.16f, 0.12f, 0.14f),
        CloudSpeed: 0.005f);

    /// <summary>Builds the sky resource for this look.</summary>
    public Sky Build()
    {
        var material = new ShaderMaterial { Shader = GD.Load<Shader>(ShaderPath) };
        material.SetShaderParameter("top_color", Top);
        material.SetShaderParameter("horizon_color", Horizon);
        material.SetShaderParameter("moon_direction", MoonDirection.Normalized());
        material.SetShaderParameter("moon_color", MoonColor);
        material.SetShaderParameter("moon_size", MoonSize);
        material.SetShaderParameter("moon_glow", MoonGlow);
        material.SetShaderParameter("star_density", StarDensity);
        material.SetShaderParameter("star_brightness", StarBrightness);
        material.SetShaderParameter("cloud_coverage", CloudCoverage);
        material.SetShaderParameter("cloud_color", CloudColor);
        material.SetShaderParameter("cloud_speed", CloudSpeed);

        return new Sky { SkyMaterial = material, ProcessMode = Sky.ProcessModeEnum.Realtime };
    }
}
