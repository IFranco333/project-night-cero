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
using GodotEnvironment = Godot.Environment;

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
/// generated town without a human watching.
/// </para>
/// </remarks>
public partial class GameRoot : Node3D
{
    private const int TicksPerFrameLimit = 8;
    private const int ScreenshotFrames = 45;

    [Export]
    public ulong Seed { get; set; } = 2026;

    private Simulation _simulation = null!;
    private TownPlan _plan = null!;
    private PlayerController _player = null!;
    private Label _hud = null!;
    private Label _crosshair = null!;
    private string? _screenshotPath;
    private bool _shadows = true;
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

        AddChild(NightEnvironment());
        AddChild(Moonlight());

        _player = new PlayerController { Name = "Player", Position = ToGodot(SpawnPoint()) };
        AddChild(_player);

        if (_pitch is not null || _distance is not null || _yaw is not null)
        {
            _player.SetPose(_yaw ?? 0f, _pitch ?? 14f, _distance ?? PlayerController.ThirdPersonDistance);
        }

        BuildHud();

        GD.Print(
            $"town seed={Seed} buildings={_plan.Buildings.Count} props={_plan.Props.Count} "
            + $"hash={_plan.Hash():x16}");
    }

    public override void _Process(double delta)
    {
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

        if (_screenshotPath is not null && ++_frames >= ScreenshotFrames)
        {
            var image = GetViewport().GetTexture().GetImage();
            image.SavePng(_screenshotPath);
            GD.Print($"screenshot {_screenshotPath}");
            GetTree().Quit();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("toggle_debug"))
        {
            _hud.Visible = !_hud.Visible;
        }
    }

    // ---- Scene setup ---------------------------------------------------------------------

    private static WorldEnvironment NightEnvironment()
    {
        var environment = new GodotEnvironment
        {
            BackgroundMode = GodotEnvironment.BGMode.Color,
            BackgroundColor = new Color(0.03f, 0.04f, 0.06f),
            AmbientLightSource = GodotEnvironment.AmbientSource.Color,
            AmbientLightColor = new Color(0.24f, 0.28f, 0.36f),
            AmbientLightEnergy = 1.15f,
            TonemapMode = GodotEnvironment.ToneMapper.Filmic,
            FogEnabled = true,
            FogLightColor = new Color(0.07f, 0.08f, 0.12f),
            FogDensity = 0.0022f,
        };

        return new WorldEnvironment { Name = "Night", Environment = environment };
    }

    private DirectionalLight3D Moonlight() => new()
    {
        Name = "Moonlight",
        LightColor = new Color(0.55f, 0.65f, 0.9f),
        LightEnergy = 0.85f,
        ShadowEnabled = _shadows,
        RotationDegrees = new Vector3(-55, 35, 0),
    };

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
            $"seed {Seed}  {_player.Mode}  mode {(_player.Aiming ? "aim" : "walk")}  "
            + $"tick {_simulation.Tick}  pos {transform.Position.X:0.0}, {transform.Position.Z:0.0}  "
            + $"fps {Engine.GetFramesPerSecond():0}  [V] camera  [H] hide";

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
