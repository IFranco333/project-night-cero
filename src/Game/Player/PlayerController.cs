using Godot;
using NightCero.Camera;
using NightCero.Core.Math;
using NightCero.Core.Player;

namespace NightCero.Game.Player;

/// <summary>Which camera the player is using. Both modes are fully playable.</summary>
public enum CameraMode
{
    Third,
    First,
}

/// <summary>
/// Turns input into a <see cref="MoveIntent"/> and drives the camera. It never moves the player
/// itself: the simulation owns the position, and this node only follows it.
/// </summary>
/// <remarks>
/// The orbit maths lives in <see cref="ThirdPersonRig"/> (engine-free, unit-tested); this class
/// only converts the resulting pose to Godot types. First person reuses the same yaw and pitch, so
/// switching modes never changes where the player is looking.
/// </remarks>
public partial class PlayerController : Node3D
{
    /// <summary>Degrees of rotation per pixel of mouse movement.</summary>
    public const float MouseSensitivity = 0.12f;

    /// <summary>Orbit radius when walking in third person, in metres.</summary>
    public const float ThirdPersonDistance = 6.5f;

    /// <summary>Orbit radius while aiming in third person, in metres.</summary>
    public const float AimDistance = 2.8f;

    /// <summary>Lateral shift of the camera while aiming, so the weapon does not cover the target.</summary>
    public const float ShoulderOffset = 0.55f;

    private const float TransitionSeconds = 0.25f;

    private float _yaw;
    private float _pitch = 14f;
    private float _transition;
    private Vector3 _lookAt;
    private bool _lookInitialised;

    /// <summary>The camera this controller drives.</summary>
    public Camera3D Camera { get; private set; } = null!;

    /// <summary>The capsule that stands in for the player in third person.</summary>
    public MeshInstance3D Avatar { get; private set; } = null!;

    /// <summary>Current camera mode.</summary>
    public CameraMode Mode { get; private set; } = CameraMode.Third;

    /// <summary>Whether the aim button is held.</summary>
    public bool Aiming { get; private set; }

    /// <summary>Horizontal angle in degrees, clockwise from north.</summary>
    public float Yaw => _yaw;

    public override void _Ready()
    {
        Camera = new Camera3D
        {
            Name = "Camera",
            Current = true,
            Fov = 70f,
            Near = 0.05f,
            Far = 2000f,
        };
        AddChild(Camera);

        Avatar = new MeshInstance3D
        {
            Name = "Avatar",
            Mesh = new CapsuleMesh { Radius = 0.35f, Height = 1.8f },
            Position = new Vector3(0, 0.9f, 0),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.34f, 0.32f, 0.29f),
                Roughness = 0.85f,
            },
        };
        AddChild(Avatar);

        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _yaw += motion.Relative.X * MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch + (motion.Relative.Y * MouseSensitivity), -80f, 80f);
            return;
        }

        if (@event.IsActionPressed("toggle_camera"))
        {
            Mode = Mode == CameraMode.Third ? CameraMode.First : CameraMode.Third;
            Avatar.Visible = Mode == CameraMode.Third;
            _transition = TransitionSeconds;
            return;
        }

        if (@event.IsActionPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            return;
        }

        if (@event is InputEventMouseButton { Pressed: true } && Input.MouseMode == Input.MouseModeEnum.Visible)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    /// <summary>Reads the keyboard and returns the intent for this frame.</summary>
    public MoveIntent ReadIntent()
    {
        Aiming = Input.IsActionPressed("aim");

        var input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        var yaw = Mathf.DegToRad(_yaw);
        var forward = new Vec2(Mathf.Sin(yaw), -Mathf.Cos(yaw));
        var right = new Vec2(Mathf.Cos(yaw), Mathf.Sin(yaw));
        var direction = (right * input.X) + (forward * -input.Y);

        return new MoveIntent(direction, Input.IsActionPressed("sprint"));
    }

    /// <summary>Places the camera for a player standing at <paramref name="feet"/>.</summary>
    public void UpdateCamera(Vec3 feet, double delta)
    {
        _transition = Mathf.Max(0f, _transition - (float)delta);

        Vector3 desiredPosition;
        Vector3 desiredLook;

        if (Mode == CameraMode.First)
        {
            var eye = ToGodot(feet) + (Vector3.Up * ThirdPersonRig.HeadHeight);
            desiredPosition = eye;
            desiredLook = eye + Forward(_yaw, _pitch);
        }
        else
        {
            var distance = Aiming ? AimDistance : ThirdPersonDistance;
            var pose = ThirdPersonRig.Evaluate(feet, _yaw, _pitch, distance);
            var shoulder = Aiming ? Right(_yaw) * ShoulderOffset : Vector3.Zero;
            desiredPosition = ToGodot(pose.Position) + shoulder;
            desiredLook = ToGodot(pose.LookAt) + shoulder;
        }

        // Third person eases so the orbit feels smooth; first person snaps to the head, except
        // during the short blend after a mode switch.
        var smooth = Mode == CameraMode.Third || _transition > 0f;
        var weight = smooth ? 1f - Mathf.Exp(-16f * (float)delta) : 1f;

        if (!_lookInitialised)
        {
            Camera.GlobalPosition = desiredPosition;
            _lookAt = desiredLook;
            _lookInitialised = true;
        }
        else
        {
            Camera.GlobalPosition = Camera.GlobalPosition.Lerp(desiredPosition, weight);
            _lookAt = _lookAt.Lerp(desiredLook, weight);
        }

        Camera.LookAt(_lookAt, Vector3.Up);
        Camera.Fov = Mathf.Lerp(Camera.Fov, Aiming ? 55f : 70f, 1f - Mathf.Exp(-10f * (float)delta));
    }

    private static Vector3 Forward(float yawDegrees, float pitchDegrees)
    {
        var yaw = Mathf.DegToRad(yawDegrees);
        var pitch = Mathf.DegToRad(pitchDegrees);
        return new Vector3(
            Mathf.Sin(yaw) * Mathf.Cos(pitch),
            -Mathf.Sin(pitch),
            -Mathf.Cos(yaw) * Mathf.Cos(pitch));
    }

    private static Vector3 Right(float yawDegrees)
    {
        var yaw = Mathf.DegToRad(yawDegrees);
        return new Vector3(Mathf.Cos(yaw), 0, Mathf.Sin(yaw));
    }

    private static Vector3 ToGodot(Vec3 value) => new(value.X, value.Y, value.Z);
}
