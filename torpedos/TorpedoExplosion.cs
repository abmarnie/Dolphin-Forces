using Godot;

namespace DolphinForces;

// TODO: Torpedo explosion destroys boats.

public partial class TorpedoExplosion : Node3D {

    [Export] AudioStreamPlayer3D _sfx = null!;
    [Export] GpuParticles3D _pfx = null!;
    float _spawnTime;

    public override void _Ready() {
        _spawnTime = Main.ElapsedTimeS();

        _sfx.Play();

        _pfx.OneShot = true;
        _pfx.Emitting = true;
    }

    public override void _PhysicsProcess(double delta) {
        const float maxLifetime = 5f;
        var isAliveForTooLong = Main.ElapsedTimeS() > _spawnTime + maxLifetime;
        if (isAliveForTooLong) {
            QueueFree();
        }
    }

}
