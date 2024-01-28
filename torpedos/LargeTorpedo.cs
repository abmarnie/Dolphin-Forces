using System.Diagnostics;
using Godot;

namespace DolphinForces;

public partial class LargeTorpedo : RigidBody3D {

    private float _lifetimeTimer;

    private bool IsUnderwater => GlobalPosition.Y <= 0;
    private AudioStreamPlayer3D _audioStreamPlayer = null!;

    private PackedScene _artefactPackedScene = GD.Load<PackedScene>("res://torpedos/torpedo_artefact.tscn");

    public override void _Ready() {
        Debug.Assert(GravityScale == 0f);
        Debug.Assert(ContactMonitor);
        Debug.Assert(MaxContactsReported >= 1);
        BodyEntered += KillBoat;
        BodyEntered += (body) => Explode();

        static void KillBoat(Node body) {
            if (body is Boat boat && !boat.IsDead) {
                boat.IsDead = true;
            }
        }

        _audioStreamPlayer = GetNode<AudioStreamPlayer3D>("%AudioStreamPlayer3D");
        _audioStreamPlayer.Play();

    }

    public override void _PhysicsProcess(double delta) {
        GravityScale = IsUnderwater ? 0.0f : 2f;

        if (!IsUnderwater) {
            const float airborneRotSpeed = 0.5f;
            Rotation -= airborneRotSpeed * (float)delta * Vector3.Right;
        }

        _lifetimeTimer += (float)delta;
        const float maxLifetime = 20f;
        if (_lifetimeTimer > maxLifetime)
            Explode();
    }

    private void Explode() {
        var artefact = _artefactPackedScene.Instantiate<TorpedoArtefact>();
        GetTree().CurrentScene.AddChild(artefact);
        artefact.GlobalPosition = GlobalPosition;
        artefact.GlobalRotation = GlobalRotation;
        QueueFree();
    }
}