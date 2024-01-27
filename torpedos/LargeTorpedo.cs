using System.Diagnostics;
using Godot;

namespace DolphinForces;

public partial class LargeTorpedo : RigidBody3D {

    private float _lifetimeTimer;

    private bool IsUnderwater => GlobalPosition.Y <= 0;

    public override void _Ready() {
        Debug.Assert(GravityScale == 0f);
        Debug.Assert(ContactMonitor);
        Debug.Assert(MaxContactsReported >= 1);
        BodyEntered += KillBoat;

        void KillBoat(Node body) {
            if (body is Boat boat && !boat.IsDead) {
                boat.IsDead = true;
                Explode();
            }
        }
    }

    public override void _PhysicsProcess(double delta) {
        _lifetimeTimer += (float)delta;
        const float maxLifetime = 20f;
        if (_lifetimeTimer > maxLifetime)
            Explode();
    }

    // TODO: Explosion sfx.
    // TODO: Explosion vfx.
    private void Explode() => QueueFree();

}