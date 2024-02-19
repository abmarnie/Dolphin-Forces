using System;
using System.Diagnostics;
using Godot;

namespace DolphinForces;

/// <summary> Controls boat enemies. </summary>
public partial class Boat : RigidBody3D {

    /// <summary> Occurs when an enemy is killed. Argument is money value
    /// of killed enemy. </summary>
    public static event Action<float>? OnKill;

    // Design parameters.
    [ExportGroup("Design Params")]
    [Export] private float _speed;
    [Export] private float _targetAcquisCooldown;
    [Export] private float _moneyIncrementOnKill;

    // Audio.
    [ExportGroup("Node Refs")]
    [Export] private AudioStreamPlayer3D _sfxPlayer = null!;

    // Dynamic art.
    [Export] private GpuParticles3D[] _smokePfxs = null!;
    private Resource _aliveTexture = ResourceLoader.Load("res://images/shared_metal_texture.png");
    private Resource _deathTexture = ResourceLoader.Load("res://nathan/destroyed_boat_texture.png");

    // Spawning.
    public bool IsAlive { get; private set; }
    private Vector3 _spawn;
    private static int _numKilled;               // For game progression.
    private static float _respawnCooldown = 10f; // For game progression.
    private float _deathTime;

    // Target acquisition.
    private static readonly Random _rng = new();
    private float _targetAcquisTime;
    private Vector3 _target;

    public override void _Ready() {
        _spawn = GlobalPosition;
        Debug.Assert(!CanSleep);
        Spawn();
    }

    public override void _PhysicsProcess(double delta) {
        if (Dolphin.CutscenePlaying) {
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
        if (Dolphin.CutscenePlaying) {
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

    public void Kill() {

        Debug.Assert(
            condition: IsAlive,
            message: $"Already dead boat was just killed."
        );

        IsAlive = false;
        _deathTime = Main.ElapsedTimeS();

        _sfxPlayer.Play();

        // Make the boat look destroyed. Some boats are composed of 
        // more than one mesh. All boat meshes share the same texture.
        var meshes = this.Descendants<MeshInstance3D>();
        meshes.ForEach(
            mesh => mesh.GetActiveMaterial(0)?
                .Set("albedo_texture", _deathTexture)
        );
        Array.ForEach(_smokePfxs, pfx => pfx.Emitting = true);

        // So that the player can watch destroyed boats spin around for fun.
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
        Dolphin.MoneyLabel.Text = $"Money Earned: ${Main.Money:N0}"; // TODO: Event.

    }

    private void SetRandomTarget() {
        _targetAcquisTime = Main.ElapsedTimeS();
        const float minDistance = 50.0f;
        const float maxDistance = 100.0f;
        var distance = ((float)_rng.NextDouble() * (maxDistance - minDistance)) + minDistance;
        var angle = (float)_rng.NextDouble() * Mathf.Pi * 2;
        var direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)).Normalized();
        _target = GlobalPosition + (direction * distance);
        LookAt(_target);
    }

    private void Spawn() {
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
        Array.ForEach(_smokePfxs, pfx => pfx.Emitting = false);
    }

}