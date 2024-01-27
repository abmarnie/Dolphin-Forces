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
    private static float ElapsedTimeS => Time.GetTicksMsec() / 1000f;
    private static Camera3D _camera = null!;

    private AnimationTree _animTree = null!;

    private Godot.Environment _underwaterEnv = null!;
    private float _mouseSens = 0.01f;
    private Vector3 _rotationFromMouse;

    private const float DEFAULT_SPEED = 25f;
    private float _speed = DEFAULT_SPEED;

    private bool IsUnderwater => GlobalPosition.Y <= 0;

    public override void _Input(InputEvent @event) {
        if (@event is InputEventMouseMotion mouseMotion && IsUnderwater) {
            _rotationFromMouse.Y -= mouseMotion.Relative.X * _mouseSens;
            _rotationFromMouse.X -= mouseMotion.Relative.Y * _mouseSens;
            _rotationFromMouse.X = Mathf.Clamp(_rotationFromMouse.X, -Mathf.Pi / 2, Mathf.Pi / 2);
        }

        if (@event.IsActionReleased("ui_cancel")) {
            Input.MouseMode = Input.MouseMode switch {
                Input.MouseModeEnum.Visible => Input.MouseModeEnum.Captured,
                Input.MouseModeEnum.Captured => Input.MouseModeEnum.Visible,
                _ => throw new NotImplementedException(),
            };
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
    }


    public override void _PhysicsProcess(double delta) {
        const float animSpeedTuningScale = 3f;
        _animTree.Set("parameters/speed_scale/scale", animSpeedTuningScale * _speed / DEFAULT_SPEED);

        if (IsUnderwater) {
            Rotation = new Vector3(_rotationFromMouse.X, _rotationFromMouse.Y, Rotation.Z);
        } else {
            const float airborneRotSpeed = 2f;
            Rotation -= airborneRotSpeed * (float)delta * Vector3.Right;
            _rotationFromMouse = Rotation;
            _animTree.Set("parameters/speed_scale/scale", 0f);
        }

        _camera.Environment = IsCameraUnderwater ? _underwaterEnv : null;

    }

    private bool _impulsedApplied;

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        if (IsUnderwater) {
            state.LinearVelocity = -_speed * Basis.Z;
            GravityScale = 0.0f;
            _impulsedApplied = false;
        } else {
            if (!_impulsedApplied)
                ApplyImpulse(-2f * _speed * Basis.Z);
            GravityScale = 9.8f;
            _impulsedApplied = true;
        }
    }

}