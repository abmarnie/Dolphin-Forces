using Godot;

namespace DolphinForces;

// TODO: Torpedo explosion destroys boats.

public partial class TorpedoExplosion : Node3D {

    [Export] AudioStreamPlayer3D _sfx = null!;
    [Export] GpuParticles3D _pfx = null!;
    float _spawnTime;

    public override void _Ready() {
        _spawnTime = Time.GetTicksMsec() / 1000f;

        _sfx.Play();

        _pfx.OneShot = true;
        _pfx.Emitting = true;
    }

    public override void _PhysicsProcess(double delta) {
        const float maxLifetime = 5f;
        var isAliveForTooLong = Time.GetTicksMsec() / 1000f > _spawnTime + maxLifetime;
        if (isAliveForTooLong) {
            QueueFree();
        }
    }

}
