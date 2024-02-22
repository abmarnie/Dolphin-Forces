using System.Diagnostics;
using Godot;

namespace DolphinForces;

public partial class FishMultimesh : MultiMeshInstance3D {

    [Export] Node3D _avoid;

    const int NUM_FISH = 2500;
    const float MIN_DEPTH = -3f;
    const float MAX_DEPTH = -100f;
    const float WANDER_DIST = 30f;

    static float RandInRange(float min, float max) =>
         (float)Main.Rng.NextDouble() * (max - min) + min;

    Vector3[] _targets = new Vector3[NUM_FISH];

    public override void _Ready() {
        Debug.Assert(_avoid is Player);
        Multimesh.InstanceCount = NUM_FISH;

        for (var fishId = 0; fishId < NUM_FISH; fishId++) {
            _targets[fishId] = RandPos(_targets[fishId], 1f, WANDER_DIST);
            _targets[fishId] = _targets[fishId] with {
                Y = Mathf.Clamp(_targets[fishId].Y, MAX_DEPTH, MIN_DEPTH)
            };

            const float initRange = 25f;
            var randOffset = new Vector3(
                x: RandInRange(-initRange, initRange),
                y: RandInRange(MAX_DEPTH, MIN_DEPTH),
                z: RandInRange(-initRange, initRange)
            );
            var randTransform = Transform3D.Identity.Translated(randOffset);
            randTransform = randTransform.LookingAt(_targets[fishId]);
            Multimesh.SetInstanceTransform(fishId, randTransform);

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
                _targets[fishId] = RandPos(_targets[fishId], 1f, WANDER_DIST);
                _targets[fishId] = _targets[fishId] with {
                    Y = Mathf.Clamp(_targets[fishId].Y, MAX_DEPTH, MIN_DEPTH)
                };

                fishTransform = fishTransform.LookingAt(_targets[fishId]);
            }

            var targetDir = fishTransform.Origin.DirectionTo(_targets[fishId]);
            const float moveAmount = 0.2f;
            var avoidanceScaleFactor =
                fishTransform.Origin.DistanceTo(_avoid.GlobalPosition) < 5f ?
                5f : 1f;
            fishTransform = fishTransform.TranslatedLocal(
                (float)Main.Rng.NextDouble() * moveAmount * avoidanceScaleFactor * targetDir);
            Multimesh.SetInstanceTransform(fishId, fishTransform);
        }
    }

    Vector3 RandPos(Vector3 startingPos, float minDist, float maxDist) {
        float randDist = RandInRange(minDist, maxDist);
        float thetaAzimuthalAngle = (float)Main.Rng.NextDouble() * 2 * Mathf.Pi;
        float phiPolarAngle = Mathf.Acos(2 * (float)Main.Rng.NextDouble() - 1);
        Vector3 randDir = new Vector3(
            Mathf.Sin(phiPolarAngle) * Mathf.Cos(thetaAzimuthalAngle),
            Mathf.Sin(phiPolarAngle) * Mathf.Sin(thetaAzimuthalAngle),
            Mathf.Cos(phiPolarAngle)
        );
        return startingPos + randDir * randDist;
    }

}
