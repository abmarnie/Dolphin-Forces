using System;
using System.Diagnostics;
using Godot;

namespace DolphinForces;

// TODO: Randomized boat spawning system (don't share texture...).

public partial class Boat : RigidBody3D {

    /// <summary> Argument is money value of killed enemy. </summary>
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
    [Export] Resource _aliveTexture = null!;
    [Export] Resource _deathTexture = null!;

    // Spawning.
    Vector3 _spawn;
    static int _numKilled;               // For game progression.
    static float _respawnCooldown = 10f; // For game progression.
    float _deathTime;
    bool _isAlive;

    // Target acquisition.
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
            if (!_isAlive || body is not (Player or Torpedo)) {
                return;
            }

            _isAlive = false;
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
        if (!Main.IsGameStarted()) {
            return;
        }

        // The boat shouldn't be affected by gravity if submerged.
        var isSubmerged = GlobalPosition.Y <= 0;
        var isDeeplySubmerged = GlobalPosition.Y <= -1f;
        GravityScale =
            _isAlive ?
                isDeeplySubmerged ? -1f
                : isSubmerged ? 0f
                : 10f
            :
                isSubmerged ? 0.25f
                : 10f;

        if (!_isAlive) {
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
        if (!Main.IsGameStarted() || !_isAlive) {
            return;
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
        var dist = (float)Main.Rng.NextDouble() * (maxDist - minDist) + minDist;
        var angle = (float)Main.Rng.NextDouble() * 2 * Mathf.Pi;
        var dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)).Normalized();
        _target = GlobalPosition + dir * dist;
        LookAt(_target);
    }

    void Spawn() {
        _isAlive = true;
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