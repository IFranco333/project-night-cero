using Godot;
using NightCero.Game.Rendering;

namespace NightCero.Game;

/// <summary>
/// The title screen: the town keeps drifting behind it, so the first thing the player sees is the
/// world they are about to walk into.
/// </summary>
/// <remarks>
/// It is built in code rather than as a <c>.tscn</c> so it needs no editor and stays diffable. The
/// node knows nothing about starting the game; it only draws itself. <see cref="GameRoot"/> owns
/// the state and watches for the key press.
/// </remarks>
public partial class TitleScreen : CanvasLayer
{
    /// <summary>Seed of the world behind the title, shown so screenshots are reproducible.</summary>
    public ulong Seed { get; set; }

    /// <summary>Project version from <c>project.godot</c>.</summary>
    public string Version { get; set; } = "0.0.0";

    /// <summary>Name of the active lighting preset, so the shot can be compared like for like.</summary>
    public string MoodName { get; set; } = "night";

    private Label _prompt = null!;
    private double _time;

    public override void _Ready()
    {
        // The world behind the title is the point; a light scrim keeps the text readable without
        // hiding it.
        var scrim = new ColorRect
        {
            Name = "Scrim",
            Color = new Color(0.01f, 0.012f, 0.02f, 0.45f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        scrim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(scrim);

        var centre = new CenterContainer { Name = "Centre" };
        centre.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        centre.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(centre);

        var column = new VBoxContainer { Name = "Column" };
        column.AddThemeConstantOverride("separation", 14);
        centre.AddChild(column);

        var title = MakeLabel("NOCHE CERO", 84, Palette.Accent);
        title.AddThemeConstantOverride("outline_size", 2);
        title.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.02f, 0.05f, 0.9f));
        column.AddChild(title);

        var subtitle = MakeLabel("A post-apocalyptic survival RPG", 22, new Color(0.74f, 0.78f, 0.86f));
        column.AddChild(subtitle);

        column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 28) });

        var meta = MakeLabel($"seed {Seed}   ·   version {Version}   ·   {MoodName}", 15, Dim());
        column.AddChild(meta);

        column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 18) });

        _prompt = MakeLabel("Press any key", 20, new Color(0.86f, 0.88f, 0.95f));
        column.AddChild(_prompt);

        var footer = MakeLabel("github.com/IFranco333/project-night-cero", 13, Dim());
        footer.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        footer.HorizontalAlignment = HorizontalAlignment.Center;
        footer.OffsetTop = -34;
        footer.OffsetBottom = -12;
        AddChild(footer);
    }

    public override void _Process(double delta)
    {
        // The prompt breathes instead of blinking hard: a full on/off blink reads as an error.
        _time += delta;
        var alpha = 0.55f + (0.45f * Mathf.Sin((float)_time * 2.4f));
        _prompt.Modulate = new Color(1f, 1f, 1f, alpha);
    }

    private static Label MakeLabel(string text, int size, Color color)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static Color Dim() => new(0.55f, 0.60f, 0.70f, 0.9f);
}
