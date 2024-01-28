using System;
using System.Diagnostics;
using Godot;

namespace DolphinForces;

public partial class Boat : RigidBody3D {

    [Export] private float _boatSpeed = 30.0f;
    [Export] private float _changeTargetInterval = 5f;

    private Vector3 _targetPosition;
    private float _newtargetPositionTimer;

    private bool IsUnderwater => GlobalPosition.Y <= 0;

    private AudioStreamPlayer3D _audioStreamPlayer = null!;
    private Random _random = new();

    private Resource _destroyed_ship_texture = ResourceLoader.Load("res://nathan/skidmark.png");

    private bool _justDied;
    private bool _isDead;
    public bool IsDead {
        get => _isDead;
        set {
            Debug.Assert(value);
            Debug.Assert(!_isDead);
            _justDied = true;
            _isDead = value;
            var meshInstances = this.Descendants<MeshInstance3D>()!;
            foreach (var mi in meshInstances) {
                var mat = mi.GetActiveMaterial(0);
                Debug.Assert(mat is not null);
                mat.Set("albedo_texture", _destroyed_ship_texture);
            }
        }
    }

    private Timer _timer = null!;

    public override void _Ready() {
        _targetPosition = GetRandomTargetPosition();
        LookAt(_targetPosition);
        ContactMonitor = true;

        // Timer crap is for sfx.
        _audioStreamPlayer = this.GetDescendant<AudioStreamPlayer3D>()!;
        _timer = new Timer();
        AddChild(_timer);
        _ = _timer.Connect("timeout", new Callable(this, nameof(OnTimerTimeout)));
        SetRandomIntervalAndStartTimer();
    }

    private void SetRandomIntervalAndStartTimer() {
        const float timerMin = 5f;
        const float timerMax = 10f;
        _timer.WaitTime = ((float)_random.NextDouble() * (timerMax - timerMin)) + timerMin;
        _timer.Start();
    }

    private void OnTimerTimeout() {
        if (_audioStreamPlayer is null) return;
        if (IsDead) return;

        if (!_audioStreamPlayer.Playing) {
            _audioStreamPlayer.Play();
            SetRandomIntervalAndStartTimer();
        }
    }

    public override void _PhysicsProcess(double delta) {
        if (IsDead) {
            _audioStreamPlayer?.Stop();
            return;
        }

        _newtargetPositionTimer += (float)delta;
        if (_newtargetPositionTimer > _changeTargetInterval) {
            SetNextTarget();
        }

        var isNearTarget = GlobalPosition.DistanceTo(_targetPosition) < 5f;
        if (isNearTarget) {
            SetNextTarget();
        }

        void SetNextTarget() {
            _targetPosition = GetRandomTargetPosition();
            LookAt(_targetPosition);
            _newtargetPositionTimer = 0;
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        GravityScale = IsUnderwater ? 0.0f : 9.8f;

        if (IsDead) {
            return;
        }

        if (GlobalPosition.DistanceTo(_targetPosition) > 1f) {
            var direction = (_targetPosition - GlobalPosition).Normalized();
            state.LinearVelocity = new Vector3(direction.X, 0, direction.Z) * _boatSpeed;
        } else {
            state.LinearVelocity = Vector3.Zero;
        }

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

    // public void LookAtInterpolate(Vector3 lookTarget, float weight, out float rotAmount) {
    //     var originalForward = -Basis.Z;
    //     var tempTransform = Transform.LookingAt(lookTarget, Vector3.Up);
    //     Transform = Transform.InterpolateWith(tempTransform, weight);
    //     var newForward = -Basis.Z;
    //     rotAmount = (float)Mathf.Acos(originalForward.Normalized().Dot(newForward.Normalized()));
    // }

}