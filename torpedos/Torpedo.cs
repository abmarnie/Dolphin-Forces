using System.Diagnostics;
using Godot;

namespace DolphinForces;

public partial class Torpedo : RigidBody3D {

    [Export] AudioStreamPlayer3D _wooshSfx = null!;
    PackedScene _explosionFactory = GD.Load<PackedScene>("res://torpedos/torpedo_explosion.tscn");
    float _spawnTime;

    public override void _Ready() {
        Debug.Assert(GravityScale == 0f);
        Debug.Assert(_explosionFactory is not null);

        _spawnTime = Main.ElapsedTimeS();
        _wooshSfx.Play();

        BodyEntered += OnBodyEntered;
        void OnBodyEntered(Node body) => Explode();
    }

    public override void _PhysicsProcess(double delta) {
        GravityScale = 0f;

        var isUnderwater = GlobalPosition.Y <= 0f;
        if (!isUnderwater) {
            const float airborneRotSpeed = 0.5f;
            Rotation -= airborneRotSpeed * (float)delta * Vector3.Right;
            GravityScale = 2f;
        }

        const float maxLifetime = 20f;
        var isAliveForTooLong = Main.ElapsedTimeS() > _spawnTime + maxLifetime;
        if (isAliveForTooLong) {
            Explode();
        }
    }


    void Explode() {
        // Leave behind a short-lasting "explosion artefact".
        var explosion = _explosionFactory.Instantiate<TorpedoExplosion>();
        GetTree().CurrentScene.AddChild(explosion);
        explosion.GlobalPosition = GlobalPosition;
        explosion.GlobalRotation = GlobalRotation;
        QueueFree();
    }

}