using System.Diagnostics;
using Godot;

namespace DolphinForces;

/// <summary> Instances the fish visuals and moves them around. </summary>
public partial class FishMultimesh : MultiMeshInstance3D {

    // TODO: Stricter type (only avoid player).
    [Export] Node3D _playerToAvoid = null!;

    // The fish shouldn't go too deep or too shallow. They are supposed to
    // sort of be used by the player to gauge depth. I probably should also
    // restrict them from straying too far from the origin.
    const float MIN_DEPTH = -3f;
    const float MAX_DEPTH = -100f;

    const int NUM_FISH = 2500;

    static float RandInRange(float min, float max) =>
         (float)Main.Rng.NextDouble() * (max - min) + min;

    Vector3[] _wanderTargets = new Vector3[NUM_FISH];

    public override void _Ready() {

        Debug.Assert(_playerToAvoid is Player);
        Debug.Assert(Multimesh.TransformFormat == MultiMesh.TransformFormatEnum.Transform3D);
        Debug.Assert(Multimesh.UseCustomData);

        Multimesh.InstanceCount = NUM_FISH;

        for (var fishId = 0; fishId < NUM_FISH; fishId++) {

            // Randomize the fish wander target.
            _wanderTargets[fishId] = RandomWanderTargetPosition(startPos: Vector3.Zero) with {
                Y = Mathf.Clamp(_wanderTargets[fishId].Y, MAX_DEPTH, MIN_DEPTH)
            };

            // Randomize the fish spawn transform.
            const float initHorzRange = 25f;
            var randTransform = Transform3D.Identity.Translated(new Vector3(
                x: RandInRange(-initHorzRange, initHorzRange),
                y: RandInRange(MAX_DEPTH, MIN_DEPTH),
                z: RandInRange(-initHorzRange, initHorzRange)
            ));
            randTransform = randTransform.LookingAt(_wanderTargets[fishId]);
            Multimesh.SetInstanceTransform(fishId, randTransform);

            // Randomize the fish
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

        for (var fishId = 0; fishId < NUM_FISH; fishId++) {
            var fishTransform = Multimesh.GetInstanceTransform(fishId);
            var isFishAtDestination = fishTransform.Origin.DistanceTo(_wanderTargets[fishId]) < 1f;
            var isChangeDirRandomly = (float)Main.Rng.NextDouble() > .01f;
            if (isFishAtDestination || isChangeDirRandomly) {
                _wanderTargets[fishId] = RandomWanderTargetPosition(_wanderTargets[fishId]);
                _wanderTargets[fishId] = _wanderTargets[fishId] with {
                    Y = Mathf.Clamp(_wanderTargets[fishId].Y, MAX_DEPTH, MIN_DEPTH)
                };

                fishTransform = fishTransform.LookingAt(_wanderTargets[fishId]);
            }

            var targetDir = fishTransform.Origin.DirectionTo(_wanderTargets[fishId]);
            const float moveAmount = 0.2f;
            var avoidanceScaleFactor =
                fishTransform.Origin.DistanceTo(_playerToAvoid.GlobalPosition) < 5f ?
                5f : 1f;
            fishTransform = fishTransform.TranslatedLocal(
                (float)Main.Rng.NextDouble() * moveAmount * avoidanceScaleFactor * targetDir);
            Multimesh.SetInstanceTransform(fishId, fishTransform);
        }
    }

    static Vector3 RandomWanderTargetPosition(Vector3 startPos) {
        const float wanderDistMin = 1f;
        const float wanderDistMax = 30f;
        var randDist = RandInRange(wanderDistMin, wanderDistMax);
        var azimuthAngle = (float)Main.Rng.NextDouble() * 2 * Mathf.Pi;
        var polarAngle = Mathf.Acos(2 * (float)Main.Rng.NextDouble() - 1);
        var randDir = new Vector3(
            Mathf.Sin(polarAngle) * Mathf.Cos(azimuthAngle),
            Mathf.Sin(polarAngle) * Mathf.Sin(azimuthAngle),
            Mathf.Cos(polarAngle)
        );
        return startPos + randDir * randDist;
    }

}
