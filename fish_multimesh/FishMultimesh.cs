using System.Diagnostics;
using Godot;

namespace DolphinForces;

/// <summary> Instances the fish visuals and moves them around. </summary>
public partial class FishMultimesh : MultiMeshInstance3D {

    // TODO: Stricter type (only avoid player).
    // TODO: Should player have a static instance?
    [Export] Node3D _player = null!;

    // The fish shouldn't go too deep or too shallow. They are supposed to
    // sort of be used by the player to gauge depth. I probably should also
    // restrict them from straying too far from the origin.
    const float MIN_DEPTH = -3f;
    const float MAX_DEPTH = -100f;

    const int NUM_FISH = 2500;

    static float RandInRange(float min, float max) =>
         (float)Main.Rng.NextDouble() * (max - min) + min;

    Vector3[] _wanderPos = new Vector3[NUM_FISH];

    public override void _Ready() {

        Debug.Assert(_player is Player);
        Debug.Assert(Multimesh.TransformFormat == MultiMesh.TransformFormatEnum.Transform3D);
        Debug.Assert(Multimesh.UseCustomData);
        Debug.Assert((Multimesh.Mesh as ArrayMesh)!.CustomAabb.Size.X > 1000f);
        Debug.Assert((Multimesh.Mesh as ArrayMesh)!.CustomAabb.Size.Y > 1000f);
        Debug.Assert((Multimesh.Mesh as ArrayMesh)!.CustomAabb.Size.Z > 1000f);

        Multimesh.InstanceCount = NUM_FISH;

        for (var fishId = 0; fishId < NUM_FISH; fishId++) {

            // Set initial fish wander target.
            _wanderPos[fishId] = RandomWanderTargetPos(startPos: Vector3.Zero);
            _wanderPos[fishId] = _wanderPos[fishId] with {
                Y = Mathf.Clamp(_wanderPos[fishId].Y, MAX_DEPTH, MIN_DEPTH)
            };

            // Set fish spawn location.
            const float initHorzRange = 25f;
            var randTransform = Transform3D.Identity.Translated(new Vector3(
                x: RandInRange(-initHorzRange, initHorzRange),
                y: RandInRange(MAX_DEPTH, MIN_DEPTH),
                z: RandInRange(-initHorzRange, initHorzRange)
            ));
            randTransform = randTransform.LookingAt(_wanderPos[fishId]);
            Multimesh.SetInstanceTransform(fishId, randTransform);

            // Prevent fish animation shaders from being perfectly synchronized.
            Multimesh.SetInstanceCustomData(fishId,
                new Color(
                    RandInRange(0, 1),
                    RandInRange(0, 1),
                    RandInRange(0, 1),
                    RandInRange(0, 1)
                )
            );

        }

    }

    public override void _PhysicsProcess(double delta) {

        if (!Main.IsGameStarted()) {
            return;
        }

        for (var id = 0; id < NUM_FISH; id++) {

            // Set the wander target randomly and look at the target. 
            var transform = Multimesh.GetInstanceTransform(id);
            var isAtDest = transform.Origin.DistanceTo(_wanderPos[id]) < 1f;
            var isChangeDirRandomly = (float)Main.Rng.NextDouble() > .01f;
            if (isAtDest || isChangeDirRandomly) {
                _wanderPos[id] = RandomWanderTargetPos(_wanderPos[id]);
                _wanderPos[id] = _wanderPos[id] with {
                    Y = Mathf.Clamp(_wanderPos[id].Y, MAX_DEPTH, MIN_DEPTH)
                };
                transform = transform.LookingAt(_wanderPos[id]);
            }

            // Move the fish a small amount. The speed of movement depends on 
            // if the player is nearby.
            var wanderDir = transform.Origin.DirectionTo(_wanderPos[id]);
            const float moveAmount = 0.2f;
            var avoidanceScaleFactor =
                transform.Origin.DistanceTo(_player.GlobalPosition) < 5f ? 5f
                : 1f;
            transform = transform.TranslatedLocal(
                (float)Main.Rng.NextDouble() * moveAmount * avoidanceScaleFactor * wanderDir);
            Multimesh.SetInstanceTransform(id, transform);

        }

    }

    static Vector3 RandomWanderTargetPos(Vector3 startPos) {
        const float minDist = 1f;
        const float maxDist = 30f;
        var randDist = RandInRange(minDist, maxDist);
        var azimuthAngle = (float)Main.Rng.NextDouble() * 2 * Mathf.Pi;
        var polarAngle = Mathf.Acos(2 * (float)Main.Rng.NextDouble() - 1);
        var randDir = new Vector3(
            Mathf.Sin(polarAngle) * Mathf.Cos(azimuthAngle),
            Mathf.Sin(polarAngle) * Mathf.Sin(azimuthAngle),
            Mathf.Cos(polarAngle)
        );
        return startPos + randDist * randDir;
    }

}
