using System;
using Godot;
using NightCero.Content;
using NightCero.Core.Math;
using NightCero.Core.Player;
using NightCero.Core.Simulation;
using NightCero.Core.WorldGen;
using NightCero.Core.WorldModel;
using NightCero.Game.Player;
using NightCero.Game.Rendering;

namespace NightCero.Game;

/// <summary>
/// Entry point of the Godot layer: generates the town, owns the <see cref="Simulation"/> and drives
/// it on a fixed tick while the frame rate varies.
/// </summary>
/// <remarks>
/// <para>
/// The only mutable state this node keeps is the render accumulator; player position comes from
/// the simulation every frame, and input leaves as a <see cref="MoveIntent"/>. Gameplay rules are
/// not allowed here (see <c>src/Game/README.md</c>).
/// </para>
/// <para>
/// Command line (<c>--</c> separates engine flags from these): <c>--seed=123</c> picks the world,
/// <c>--screenshot=path.png</c> renders a few frames, saves an image and quits — used to check the
/// generated town without a human watching. A screenshot run starts in the town; add
/// <c>--title</c> to capture the title screen instead.
/// </para>
/// </remarks>
public partial class GameRoot : Node3D
{
    private const int TicksPerFrameLimit = 8;
    private const int ScreenshotFrames = 45;

    /// <summary>Degrees per second the title screen orbits the town.</summary>
    private const float TitleOrbitSpeed = 3.5f;

    [Export]
    public ulong Seed { get; set; } = 2026;

    private Simulation _simulation = null!;
    private TownPlan _plan = null!;
    private PlayerController _player = null!;
    private Node3D _lighting = null!;
    private Label _hud = null!;
    private Label _crosshair = null!;
    private TitleScreen? _title;
    private string? _screenshotPath;
    private bool _shadows = true;
    private bool _started;
    private bool _keepTitle;
    private bool _poseGiven;
    private float _titleYaw;
    private float _titleSeconds;
    private Mood _mood = Mood.Night;
    private float? _yaw;
    private float? _pitch;
    private float? _distance;
    private double _accumulator;
    private int _frames;

    public override void _Ready()
    {
        ParseArguments();

        _plan = TownGenerator.Generate(Seed, TownSettings.Default, BuildingCatalog.All);

        _simulation = new Simulation(Seed);
        TownSpawner.Spawn(_simulation.World, _plan);
        var player = _simulation.World.Spawn(
            new Transform(SpawnPoint(), 0f),
            new PlayerMoveState(Vec2.Zero, false));
        _simulation.BindPlayer(player);
        _simulation.World.AddSystem(new PlayerMovementSystem());

        var world = new WorldRenderer { Name = "World" };
        AddChild(world);
        world.Build(_plan);

        _lighting = Lighting();
        AddChild(_lighting);

        _player = new PlayerController { Name = "Player", Position = ToGodot(SpawnPoint()) };
        AddChild(_player);

        if (_pitch is not null || _distance is not null || _yaw is not null)
        {
            _player.SetPose(_yaw ?? 0f, _pitch ?? 14f, _distance ?? PlayerController.ThirdPersonDistance);
        }

        BuildHud();

        _poseGiven = _yaw is not null || _pitch is not null || _distance is not null;

        // A screenshot run is an inspection run: it starts in the town unless the caller asked for
        // the title screen on purpose.
        if (_screenshotPath is not null && !_keepTitle)
        {
            StartGame();
        }
        else
        {
            ShowTitle();
        }

        GD.Print(
            $"town seed={Seed} buildings={_plan.Buildings.Count} props={_plan.Props.Count} "
            + $"hash={_plan.Hash():x16}");
    }

    public override void _Process(double delta)
    {
        if (!_started)
        {
            // The title screen is a menu, not a pause: the simulation has not started, and the
            // camera drifts around the spawn point so the town is visible behind the text.
            _titleYaw += (float)delta * TitleOrbitSpeed;
            _titleSeconds += (float)delta;
            _player.SetPose(_titleYaw, 22f, 70f);
            var spawn = _simulation.World.Get<Transform>(_simulation.Player);
            _player.Position = ToGodot(spawn.Position);
            _player.UpdateCamera(spawn.Position, delta);
            CaptureScreenshotIfDue();
            return;
        }

        _simulation.Submit(_player.ReadIntent());

        _accumulator += delta;
        var step = SimulationClock.TickDuration.TotalSeconds;
        var ticks = 0;
        while (_accumulator >= step && ticks < TicksPerFrameLimit)
        {
            _simulation.Run(1);
            _accumulator -= step;
            ticks++;
        }

        var transform = _simulation.World.Get<Transform>(_simulation.Player);
        _player.Position = ToGodot(transform.Position);
        _player.UpdateCamera(transform.Position, delta);
        UpdateHud(transform);

        CaptureScreenshotIfDue();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_started)
        {
            // --title is an inspection mode: the title stays up so a screenshot is reproducible.
            if (_keepTitle)
            {
                return;
            }

            // The window can deliver a stray key event in its first frames (at least on the GL
            // Compatibility backend); ignore the first half second so the title is not skipped
            // before it is drawn.
            if (_titleSeconds < 0.5f)
            {
                return;
            }

            // Any deliberate input starts the game. Escape is excluded: it is the release-mouse key
            // once the game is running, and starting on it would be a surprise.
            var start = @event switch
            {
                InputEventKey { Pressed: true, Echo: false } key => key.Keycode != Key.Escape,
                InputEventMouseButton { Pressed: true } => true,
                InputEventJoypadButton { Pressed: true } => true,
                _ => false,
            };

            if (start)
            {
                StartGame();
            }

            return;
        }

        if (@event.IsActionPressed("toggle_debug"))
        {
            _hud.Visible = !_hud.Visible;
        }

        if (@event.IsActionPressed("cycle_mood"))
        {
            SetMood(Mood.Next(_mood.Name));
        }
    }

    // ---- Scene setup ---------------------------------------------------------------------

    /// <summary>Shows the title over the drifting town and leaves the simulation paused.</summary>
    private void ShowTitle()
    {
        _hud.Visible = false;
        _player.Avatar.Visible = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;

        _title = new TitleScreen
        {
            Name = "Title",
            Seed = Seed,
            Version = ProjectSettings.GetSetting("application/config/version", "0.0.0").AsString(),
            MoodName = _mood.Name,
        };
        AddChild(_title);
    }

    /// <summary>Leaves the title screen and hands control to the player.</summary>
    private void StartGame()
    {
        _started = true;

        if (_title is not null)
        {
            _title.QueueFree();
            _title = null;
        }

        _hud.Visible = true;
        _player.Avatar.Visible = _player.Mode == CameraMode.Third;
        Input.MouseMode = Input.MouseModeEnum.Captured;

        // The title orbits at inspection distance; starting returns to the gameplay view, unless the
        // caller pinned a pose on the command line.
        _player.SetPose(
            _poseGiven ? _yaw ?? 0f : 0f,
            _poseGiven ? _pitch ?? 14f : 14f,
            _poseGiven ? _distance ?? PlayerController.ThirdPersonDistance : PlayerController.ThirdPersonDistance);

        GD.Print("start");
    }

    private void CaptureScreenshotIfDue()
    {
        if (_screenshotPath is null || ++_frames < ScreenshotFrames)
        {
            return;
        }

        var image = GetViewport().GetTexture().GetImage();
        image.SavePng(_screenshotPath);
        GD.Print($"screenshot {_screenshotPath}");
        GetTree().Quit();
    }

    private void SetMood(Mood mood)
    {
        _mood = mood;
        RemoveChild(_lighting);
        _lighting.QueueFree();
        _lighting = Lighting();
        AddChild(_lighting);
        GD.Print($"mood {mood.Name}");
    }

    /// <summary>
    /// Sky, fog and key light for the current <see cref="Mood"/>. Both are parented to a plain node
    /// so the pair can be swapped without touching the town.
    /// </summary>
    private Node3D Lighting()
    {
        var group = new Node3D { Name = "Lighting" };
        group.AddChild(new WorldEnvironment { Name = "Environment", Environment = _mood.BuildEnvironment() });
        group.AddChild(_mood.BuildKeyLight(_shadows));
        return group;
    }

    private void BuildHud()
    {
        var layer = new CanvasLayer { Name = "Hud" };

        _hud = new Label
        {
            Name = "Debug",
            Position = new Vector2(16, 12),
        };
        _hud.AddThemeFontSizeOverride("font_size", 14);
        _hud.AddThemeColorOverride("font_color", new Color(0.82f, 0.88f, 0.92f));
        layer.AddChild(_hud);

        _crosshair = new Label
        {
            Name = "Crosshair",
            Text = "+",
            Visible = false,
        };
        _crosshair.AddThemeFontSizeOverride("font_size", 20);
        _crosshair.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.85f, 0.8f));
        _crosshair.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _crosshair.HorizontalAlignment = HorizontalAlignment.Center;
        _crosshair.VerticalAlignment = VerticalAlignment.Center;
        layer.AddChild(_crosshair);

        AddChild(layer);

        if (Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            _hud.Text = "Click to capture the mouse. WASD move, Shift sprint, RMB aim, V camera, Esc release.";
        }
    }

    private void UpdateHud(Transform transform)
    {
        _hud.Text =
            $"seed {Seed}  {_mood.Name}  {_player.Mode}  mode {(_player.Aiming ? "aim" : "walk")}  "
            + $"tick {_simulation.Tick}  pos {transform.Position.X:0.0}, {transform.Position.Z:0.0}  "
            + $"fps {Engine.GetFramesPerSecond():0}  [V] camera  [M] mood  [H] hide";

        _crosshair.Visible = _player.Mode == CameraMode.First || _player.Aiming;
    }

    // ---- Arguments and helpers -----------------------------------------------------------

    private void ParseArguments()
    {
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith("--seed=", StringComparison.Ordinal))
            {
                if (ulong.TryParse(argument[7..], out var seed))
                {
                    Seed = seed;
                }
            }
            else if (argument == "--no-shadows")
            {
                _shadows = false;
            }
            else if (argument == "--title")
            {
                _keepTitle = true;
            }
            else if (argument.StartsWith("--yaw=", StringComparison.Ordinal))
            {
                _yaw = ParseFloat(argument[6..]);
            }
            else if (argument.StartsWith("--pitch=", StringComparison.Ordinal))
            {
                _pitch = ParseFloat(argument[8..]);
            }
            else if (argument.StartsWith("--distance=", StringComparison.Ordinal))
            {
                _distance = ParseFloat(argument[11..]);
            }
            else if (argument.StartsWith("--mood=", StringComparison.Ordinal))
            {
                var mood = Mood.For(argument[7..]);
                if (mood is null)
                {
                    GD.PushWarning($"unknown mood '{argument[7..]}'; use night, overcast or dusk");
                }
                else
                {
                    _mood = mood;
                }
            }
            else if (argument.StartsWith("--screenshot=", StringComparison.Ordinal))
            {
                var path = argument[13..];
                _screenshotPath = path.Length > 0
                    ? path
                    : System.IO.Path.Combine(OS.GetUserDataDir(), "preview.png");
            }
        }
    }

    private static float? ParseFloat(string text) =>
        float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    /// <summary>The centre of the town snapped to a road crossing, so the player starts on asphalt.</summary>
    private Vec3 SpawnPoint()
    {
        var x = MathF.Round(_plan.Width / 2f / _plan.BlockPitch) * _plan.BlockPitch;
        var z = MathF.Round(_plan.Height / 2f / _plan.BlockPitch) * _plan.BlockPitch;
        return new Vec3(x, 0, z);
    }

    private static Vector3 ToGodot(Vec3 value) => new(value.X, value.Y, value.Z);
}
