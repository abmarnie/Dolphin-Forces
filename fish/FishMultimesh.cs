using System.Diagnostics;
using Godot;

namespace DolphinForces;

public partial class FishMultimesh : MultiMeshInstance3D {

    const int NUM_FISH = 2000;
    const float MIN_DEPTH = 0f;
    const float MAX_DEPTH = -100f;
    const float WANDER_DEPTH = 30f;
    const float WANDER_XY_DIST = 30f;

    static float RandInRange(float min, float max) =>
         (float)Main.Rng.NextDouble() * (max - min) + min;

    Vector3[] _targets = new Vector3[NUM_FISH];

    public override void _Ready() {
        Multimesh.InstanceCount = NUM_FISH;

        for (var fishId = 0; fishId < NUM_FISH; fishId++) {
            const float initRange = 25f;
            var randOffset = new Vector3(
                x: RandInRange(-initRange, initRange),
                y: RandInRange(MAX_DEPTH, MIN_DEPTH),
                z: RandInRange(-initRange, initRange)
            );

            _targets[fishId] = RandPos(_targets[fishId], 1f, WANDER_XY_DIST, WANDER_DEPTH);
            _targets[fishId] = _targets[fishId] with {
                Y = Mathf.Clamp(_targets[fishId].Y, MAX_DEPTH, MIN_DEPTH)
            };

            Multimesh.SetInstanceTransform(fishId, Transform3D.Identity.Translated(randOffset));

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
            var isFishAtDestination = fishTransform.Origin.DistanceTo(_targets[fishId]) < 1f;
            var isChangeDirRandomly = (float)Main.Rng.NextDouble() > .01f;
            if (isFishAtDestination || isChangeDirRandomly) {
                _targets[fishId] = RandPos(_targets[fishId], 1f, WANDER_XY_DIST, WANDER_DEPTH);
                _targets[fishId] = _targets[fishId] with {
                    Y = Mathf.Clamp(_targets[fishId].Y, MAX_DEPTH, MIN_DEPTH)
                };

                fishTransform = fishTransform.LookingAt(_targets[fishId]);
            }

            var targetDir = fishTransform.Origin.DirectionTo(_targets[fishId]);
            const float moveAmount = 0.2f;
            fishTransform = fishTransform.TranslatedLocal(
                (float)Main.Rng.NextDouble() * moveAmount * targetDir);
            Multimesh.SetInstanceTransform(fishId, fishTransform);
        }
    }

    Vector3 RandPos(Vector3 startingPos, float minDist, float maxDist, float heightRange) {
        var randDist = RandInRange(minDist, maxDist);
        var randHeight = RandInRange(0f, heightRange);
        var randAngle = (float)Main.Rng.NextDouble() * 2 * Mathf.Pi;
        var randDir = new Vector3(Mathf.Cos(randAngle), 0, Mathf.Sin(randAngle)).Normalized();
        return startingPos + (randHeight * Vector3.Up) + (randDir * randDist);
    }

}
