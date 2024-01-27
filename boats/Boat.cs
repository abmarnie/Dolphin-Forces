using System;
using Godot;

namespace DolphinForces;

public partial class Boat : RigidBody3D {

    private Vector3 _targetPosition;
    private float _speed = 30.0f;
    private float _changeTargetInterval = 5f;
    private float _timer;

    public override void _Ready() => _targetPosition = GetRandomTargetPosition();

    public override void _PhysicsProcess(double delta) {
        _timer += (float)delta;
        if (_timer > _changeTargetInterval) {
            _targetPosition = GetRandomTargetPosition();
            _timer = 0;
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        var direction = (_targetPosition - GlobalPosition).Normalized();
        state.LinearVelocity = new Vector3(direction.X, 0, direction.Z) * _speed;
    }

    private Vector3 GetRandomTargetPosition() {
        var rng = new Random();
        const float minDistance = 50.0f;
        const float maxDistance = 100.0f;
        var distance = ((float)rng.NextDouble() * (maxDistance - minDistance)) + minDistance;

        var angle = (float)rng.NextDouble() * Mathf.Pi * 2; // Random angle in radians
        var direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)).Normalized();

        return GlobalPosition + (direction * distance);
    }
}
