using System;
using System.Diagnostics;
using Godot;

namespace DolphinForces;

// TODO: Adjust dolphin roll when turning for flourish.
// TODO: Maintain angular velocity when launching out of water for flourish.
// TODO: Speed boost.

public partial class Dolphin : RigidBody3D {

    private static float ElapsedTimeS => Time.GetTicksMsec() / 1000f;

    [Export] private AnimationTree _animTree = null!;
    [Export] private Camera3D _camera = null!;

    private Godot.Environment _underwaterEnv = null!;
    private const float DEFAULT_SPEED = 15f;
    private float _speed = DEFAULT_SPEED;
    private float _mouseSens = 0.01f;
    private Vector3 _rotationFromMouse;
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
        if (@event.IsActionReleased("scroll_up")) {
            _speed += speedScrollDelta;
        }

        if (@event.IsActionReleased("scroll_down")) {
            _speed -= speedScrollDelta;
        }

        const float minSpeed = 5f;
        const float maxSpeed = 25f;
        _speed = Mathf.Clamp(_speed, minSpeed, maxSpeed);
    }

    public override void _Ready() {
        Debug.Assert(_animTree is not null);
        _animTree.Set("parameters/speed_scale/scale", 1f);
        _underwaterEnv = (Godot.Environment)GD.Load("res://underwater_environment.tres");
        Debug.Assert(_underwaterEnv is not null);
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }


    public override void _PhysicsProcess(double delta) {
        _animTree.Set("parameters/speed_scale/scale", _speed / DEFAULT_SPEED);

        if (IsUnderwater) {
            Rotation = new Vector3(_rotationFromMouse.X, _rotationFromMouse.Y, Rotation.Z);
        } else {
            const float rotSpeed = 1f;
            Rotation -= rotSpeed * (float)delta * Vector3.Right;
            _rotationFromMouse = Rotation;
        }

        var isCameraUnderwater = _camera.GlobalPosition.Y <= -0.3f;
        _camera.Environment = isCameraUnderwater ? _underwaterEnv : null;
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        if (IsUnderwater) {
            state.LinearVelocity = -_speed * Basis.Z;
            GravityScale = 0.0f;
        } else {
            // state.AngularVelocity = -Basis.X;
            GravityScale = 1.0f;
        }
    }

}