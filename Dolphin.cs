using System.Diagnostics;
using Godot;

namespace DolphinForces;

public partial class Dolphin : RigidBody3D {

    [Export] private AnimationTree _animTree = null!;
    [Export] private Camera3D _camera = null!;

    private Environment _underwaterEnv = null!;
    private float _moveSpeed = 15f;
    private float _animSpeedScale = 1f;
    private float _mouseSens = 0.01f;
    private Vector3 _rotationFromMouseInput;


    public override void _Input(InputEvent @event) {
        if (@event is InputEventMouseMotion mouseMotion) {
            _rotationFromMouseInput.Y -= mouseMotion.Relative.X * _mouseSens;
            _rotationFromMouseInput.X -= mouseMotion.Relative.Y * _mouseSens;
            _rotationFromMouseInput.X = Mathf.Clamp(_rotationFromMouseInput.X, -Mathf.Pi / 2, Mathf.Pi / 2);
        }
    }

    public override void _Ready() {
        Debug.Assert(_animTree is not null);
        _animTree.Set("parameters/speed_scale/scale", _animSpeedScale);
        _underwaterEnv = (Environment)GD.Load("res://underwater_environment.tres");
        Debug.Assert(_underwaterEnv is not null);
    }

    // TODO: Maintain angular velocity when launching out of water.
    // TODO: Speed boost.

    public override void _PhysicsProcess(double delta) {
        Rotation = new Vector3(_rotationFromMouseInput.X, _rotationFromMouseInput.Y, Rotation.Z);
        // GD.Print(_underwaterEnv is null);
        var isCameraUnderwater = _camera.GlobalPosition.Y <= -0.5f;
        _camera.Environment = isCameraUnderwater ? _underwaterEnv : null;
        // _camera.Environment = _underwaterEnv;
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        var isUnderwater = GlobalPosition.Y <= 0;
        if (isUnderwater) {
            state.LinearVelocity = -_moveSpeed * Basis.Z;
            GravityScale = 0.0f;
        } else {
            GravityScale = 1.0f;
        }
    }
}



