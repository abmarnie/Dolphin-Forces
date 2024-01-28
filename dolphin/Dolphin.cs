using System;
using System.Diagnostics;
using Godot;

namespace DolphinForces;

// TODO: Adjust dolphin roll when turning for flourish.
// TODO: Speed boost ability.
// TODO: Torpedos.
// TODO: Settings menu (mouse sensitivity slider).
// TODO: Sounds menu.

public partial class Dolphin : RigidBody3D {

    public static bool IsCameraUnderwater => _camera.GlobalPosition.Y <= -0.3f;
    private static Camera3D _camera = null!;

    public event Action? OnJump;
    public event Action? OnWaterEntry;
    public bool IsUnderwater => GlobalPosition.Y <= 0;

    private AnimationTree _animTree = null!;

    private Godot.Environment _underwaterEnv = null!;
    private float _mouseSens = 0.01f;
    private Vector3 _rotationFromMouse;

    private const float DEFAULT_SPEED = 25f;
    private float _speed = DEFAULT_SPEED;

    private const float LARGE_TORPEDO_COOLDOWN = 0.1f;
    private PackedScene _largeTorpedoPackedScene = GD.Load<PackedScene>("res://torpedos/large_torpedo.tscn");
    private Node3D _largeTorpedoSpawnLocation = null!;
    private float _lastLargeTorpedoFireTime;


    private AudioStream _robot_initial_load_sfx = null!;
    private AudioStream _splash_sfx = null!;
    private bool _isSplashSoundLoaded;

    private AudioStreamPlayer3D _audioStreamPlayer = null!;

    public override void _Input(InputEvent @event) {
        if (@event is InputEventMouseMotion mouseMotion && IsUnderwater) {
            _rotationFromMouse.Y -= mouseMotion.Relative.X * _mouseSens;
            _rotationFromMouse.X -= mouseMotion.Relative.Y * _mouseSens;
            var lookAngleBounds = Mathf.DegToRad(75.0f);
            _rotationFromMouse.X = Mathf.Clamp(_rotationFromMouse.X, -lookAngleBounds, lookAngleBounds);
        }

        if (@event.IsActionReleased("ui_cancel")) {
            Input.MouseMode = Input.MouseMode switch {
                Input.MouseModeEnum.Visible => Input.MouseModeEnum.Captured,
                Input.MouseModeEnum.Captured => Input.MouseModeEnum.Visible,
                _ => throw new NotImplementedException(),
            };
        }

        var isTorpedoOffCooldown = Main.ElapsedTimeS > _lastLargeTorpedoFireTime + LARGE_TORPEDO_COOLDOWN;
        if (@event.IsActionReleased("attack") && isTorpedoOffCooldown) { // TODO: Cooldown bar async fill.
            var torpedo = _largeTorpedoPackedScene.Instantiate<LargeTorpedo>();
            GetTree().CurrentScene.AddChild(torpedo);
            torpedo.AddCollisionExceptionWith(this);
            torpedo.GlobalPosition = _largeTorpedoSpawnLocation.GlobalPosition;
            torpedo.GlobalRotation = _largeTorpedoSpawnLocation.GlobalRotation;
            var torpedoForce = 40f + _speed;
            torpedo.ApplyImpulse(-torpedoForce * torpedo.Basis.Z);
            _lastLargeTorpedoFireTime = Main.ElapsedTimeS;
        }

        const float speedScrollDelta = 1f;
        if (@event.IsActionReleased("scroll_up"))
            _speed += speedScrollDelta;
        else if (@event.IsActionReleased("scroll_down"))
            _speed -= speedScrollDelta;

        const float minSpeed = 5f;
        const float maxSpeed = 50f;
        _speed = Mathf.Clamp(_speed, minSpeed, maxSpeed);
    }

    public override void _Ready() {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _camera = GetNode<Camera3D>("%Camera3D");

        _animTree = GetNode<AnimationTree>("%AnimationTree");
        Debug.Assert(_animTree is not null);
        _animTree.Set("parameters/speed_scale/scale", 1f);

        _underwaterEnv = (Godot.Environment)GD.Load("res://water/underwater_environment.tres");
        Debug.Assert(_underwaterEnv is not null);

        Debug.Assert(ContactMonitor);
        Debug.Assert(MaxContactsReported >= 1);
        BodyEntered += KillBoat;

        static void KillBoat(Node body) {
            if (body is Boat boat && !boat.IsDead) {
                boat.IsDead = true;
            }
        }

        _lastLargeTorpedoFireTime = -LARGE_TORPEDO_COOLDOWN;
        _largeTorpedoSpawnLocation = GetNode<Node3D>("%LargeTorpedoSpawnLocation");
        Debug.Assert(_largeTorpedoSpawnLocation is not null);

        _audioStreamPlayer = GetNode<AudioStreamPlayer3D>("%AudioStreamPlayer3D");
        _audioStreamPlayer.Play();
        _audioStreamPlayer.Finished += LoadSplashSound;
    }


    private void LoadSplashSound() {
        if (_isSplashSoundLoaded) {
            _audioStreamPlayer.Finished -= LoadSplashSound;
            return;
        }
        _splash_sfx = ResourceLoader.Load<AudioStream>("res://nathan/splash.mp3");
        _audioStreamPlayer.Stream = _splash_sfx;
    }

    public override void _PhysicsProcess(double delta) {
        const float defaultFov = 75;
        const float fovScalingFactor = 0.25f; // Adjust this value as needed
        _camera.Fov = defaultFov + (defaultFov * (_speed - DEFAULT_SPEED) / DEFAULT_SPEED * fovScalingFactor);

        const float animSpeedTuningScale = 3f;
        _animTree.Set("parameters/speed_scale/scale", animSpeedTuningScale * _speed / DEFAULT_SPEED);

        if (IsUnderwater) {
            Rotation = new Vector3(_rotationFromMouse.X, _rotationFromMouse.Y, Rotation.Z);
        } else {
            const float airborneRotSpeed = 2f;
            Rotation -= airborneRotSpeed * (float)delta * Vector3.Right;

            var radians75 = Mathf.DegToRad(75.0f);
            Rotation = Rotation with { X = Mathf.Clamp(Rotation.X, -radians75, radians75) };
            // Rotation = new Vector3(Mathf.Clamp(Rotation.X, -radians75, radians75), Rotation.Y, Rotation.Y);

            _rotationFromMouse = Rotation;
            _animTree.Set("parameters/speed_scale/scale", 0f);
        }

        _camera.Environment = IsCameraUnderwater ? _underwaterEnv : null;

    }

    private bool _impulsedApplied;

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {

        if (IsUnderwater) {
            if (_impulsedApplied) {
                _audioStreamPlayer.Play();
                OnWaterEntry?.Invoke();
            }
            state.LinearVelocity = -_speed * Basis.Z;
            GravityScale = 0.0f;
            _impulsedApplied = false;
        } else {
            if (!_impulsedApplied) {
                _audioStreamPlayer.Play();
                OnJump?.Invoke();
                ApplyImpulse(-2f * _speed * Basis.Z);
            }
            GravityScale = 9.8f;
            _impulsedApplied = true;
        }

    }

}