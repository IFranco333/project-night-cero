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

        // Two words, two tones: the noun stays neutral and the accent lands on "ZERO", which is
        // also the one colour the rest of the game uses for the things that matter at night.
        var words = new HBoxContainer
        {
            Name = "Words",
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        words.AddThemeConstantOverride("separation", 24);
        column.AddChild(words);
        words.AddChild(MakeTitleWord("NIGHT", new Color(0.86f, 0.88f, 0.96f)));
        words.AddChild(MakeTitleWord("ZERO", Palette.AccentBright));

        var rule = new ColorRect
        {
            Name = "Rule",
            Color = new Color(Palette.Accent.R, Palette.Accent.G, Palette.Accent.B, 0.55f),
            CustomMinimumSize = new Vector2(320, 2),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        column.AddChild(rule);

        var subtitle = MakeLabel("A post-apocalyptic survival RPG", 22, new Color(0.74f, 0.78f, 0.86f));
        column.AddChild(subtitle);

        column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 24) });

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

    /// <summary>A title word: large, with a soft shadow so it holds over the lit windows behind.</summary>
    private static Label MakeTitleWord(string text, Color color)
    {
        var label = MakeLabel(text, 92, color);
        label.AddThemeConstantOverride("shadow_offset_x", 0);
        label.AddThemeConstantOverride("shadow_offset_y", 5);
        label.AddThemeConstantOverride("shadow_outline_size", 6);
        label.AddThemeColorOverride("font_shadow_color", new Color(0.02f, 0.02f, 0.06f, 0.75f));
        return label;
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
