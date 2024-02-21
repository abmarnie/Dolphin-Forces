using System;
using System.Diagnostics;
using Godot;

namespace DolphinForces;

// TODO: Randomized boat spawning system (don't share texture...).
// TODO: Boat "type object" determines speed & targetAquisCooldown.
// TODO: Better randomized target behaviour..

/// <summary> Controls boat enemies. </summary>
public partial class Boat : RigidBody3D {

    /// <summary> Occurs when an enemy is killed. Argument is money value
    /// of killed enemy. </summary>
    public static event Action<float>? OnKill;

    // Design parameters.
    [ExportGroup("Design Params")]
    [Export] float _speed;
    [Export] float _targetAcquisCooldown;
    [Export] float _moneyIncrementOnKill;

    // Audio.
    [ExportGroup("Node Refs")]
    [Export] AudioStreamPlayer3D _sfxPlayer = null!;

    // Dynamic art.
    [Export] GpuParticles3D[] _deathPfxs = null!;
    Resource _aliveTexture = GD.Load("res://images/shared_metal_texture.png");
    Resource _deathTexture = GD.Load("res://nathan/destroyed_boat_texture.png");

    // Spawning.
    public bool IsAlive { get; private set; }
    Vector3 _spawn;
    static int _numKilled;               // For game progression.
    static float _respawnCooldown = 10f; // For game progression.
    float _deathTime;

    // Target acquisition.
    static readonly Random _rng = new();
    float _targetAcquisTime;
    Vector3 _target;

    public override void _Ready() {
        Debug.Assert(!CanSleep);
        Debug.Assert(ContactMonitor);
        Debug.Assert(MaxContactsReported >= 1);
        Debug.Assert(_deathPfxs is not null);
        Debug.Assert(_deathPfxs.Length > 0, $"{Name}");

        foreach (var deathPfx in _deathPfxs) {
            Debug.Assert(deathPfx is not null);
        }

        _spawn = GlobalPosition;
        Spawn();

        BodyEntered += OnBodyEntered;

        void OnBodyEntered(Node body) {
            // The boat should *only* be killed by collision from the Player
            // or from a Torpedo.
            if (!IsAlive || body is not (Player or Torpedo)) {
                return;
            }

            IsAlive = false;
            _deathTime = Main.ElapsedTimeS();

            _sfxPlayer.Play();

            // Make the boat look destroyed. Some boats are composed of 
            // more than one mesh. All boat meshes use the same texture.
            var meshes = this.Descendants<MeshInstance3D>();
            meshes.ForEach(
                mesh => mesh.GetActiveMaterial(0)?
                    .Set("albedo_texture", _deathTexture)
            );
            Array.ForEach(_deathPfxs, pfx => pfx.Emitting = true);

            // So player can ragdoll boats for fun.
            AxisLockAngularX = false;
            AxisLockAngularY = false;
            AxisLockAngularZ = false;

            // Give player more targets to destroy as they progress.
            _numKilled++;
            if (_numKilled % 30 == 0) {
                _respawnCooldown *= 0.9f;
            }

            // Money is "score". Used for infinite progression.
            OnKill?.Invoke(_moneyIncrementOnKill);
        }
    }

    public override void _PhysicsProcess(double delta) {
        if (Player.IsIntroPlaying()) {
            return;
        }

        if (!IsAlive) {
            var isRespawnOffCooldown = Main.ElapsedTimeS() >= _deathTime + _respawnCooldown;
            if (isRespawnOffCooldown) {
                Spawn();
            }
            return;
        }

        var targetAcquisOffCooldown = _targetAcquisTime >= Main.ElapsedTimeS() + _targetAcquisCooldown;
        var isNearTarget = GlobalPosition.DistanceTo(_target) < 5f;
        if (targetAcquisOffCooldown || isNearTarget) {
            SetRandomTarget();
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        if (Player.IsIntroPlaying()) {
            return;
        }

        // HACK: Cancel gravity if submerged to simulate buoyancy.
        var isSubmerged = GlobalPosition.Y <= 0;
        GravityScale = isSubmerged ? 0.0f : 9.8f;

        if (!IsAlive) {
            return;
        }

        // TODO: Boats can push each other down, leading to some boats becoming
        // stuck underwater (instead of at the surface). Below is a low-effort 
        // hack to try to fix this, which doesn't seem to work. 

        var isReallySubmerged = GlobalPosition.Y < -0.5f;
        if (isReallySubmerged) {
            var upwardForceToPreventJank = new Vector3(0, 10.0f, 0);
            state.ApplyCentralImpulse(upwardForceToPreventJank);
        }

        // Move towards the target direction.
        var isNearTarget = GlobalPosition.DistanceTo(_target) <= 1f;
        var targetDir = GlobalPosition.DirectionTo(_target);
        state.LinearVelocity = isNearTarget ? Vector3.Zero :
            new Vector3(targetDir.X, 0, targetDir.Z) * _speed;
    }

    void SetRandomTarget() {
        _targetAcquisTime = Main.ElapsedTimeS();
        const float minDist = 50.0f;
        const float maxDist = 100.0f;
        var dist = ((float)_rng.NextDouble() * (maxDist - minDist)) + minDist;
        var angle = (float)_rng.NextDouble();
        var dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)).Normalized();
        _target = GlobalPosition + (dir * dist);
        LookAt(_target);
    }

    void Spawn() {
        IsAlive = true;
        GlobalPosition = _spawn;
        SetRandomTarget();

        AxisLockAngularX = true;
        AxisLockAngularY = true;
        AxisLockAngularZ = true;

        var meshes = this.Descendants<MeshInstance3D>();
        meshes.ForEach(
            mesh => mesh.GetActiveMaterial(0)?
                .Set("albedo_texture", _aliveTexture)
        );
        Array.ForEach(_deathPfxs, pfx => pfx.Emitting = false);
    }

}