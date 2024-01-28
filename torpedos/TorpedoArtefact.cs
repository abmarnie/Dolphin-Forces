using System.Diagnostics;
using Godot;

namespace DolphinForces;

// The crap left behind when a torpedo dies.
public partial class TorpedoArtefact : Node3D {

    private float _lifetimeTimer;

    public override void _Ready() {
        var audioPlayer = this.GetDescendant<AudioStreamPlayer3D>();
        Debug.Assert(audioPlayer is not null);
        audioPlayer.Play();
        var explosionParticle = this.GetDescendant<GpuParticles3D>();
        Debug.Assert(explosionParticle is not null);
        explosionParticle.OneShot = true;
        explosionParticle.Emitting = true;
    }

    public override void _PhysicsProcess(double delta) {
        _lifetimeTimer += (float)delta;
        const float maxLifetime = 5f;
        if (_lifetimeTimer > maxLifetime)
            QueueFree();

    }
}
