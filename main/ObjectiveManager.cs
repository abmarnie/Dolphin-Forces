using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

namespace DolphinForces;

public partial class ObjectiveManager : Node3D {

    // [Export] private ObjectiveManager _nextObjective = null!;
    // private List<Boat> _boats = null!;

    // public override void _Ready() {
    //     _boats = this.Descendants<Boat>();
    //     Debug.Assert(_boats is not null);
    // }

    // public override void _PhysicsProcess(double delta) {
    //     var isAllDead = _boats.All(boat => boat.IsDead);
    //     if (isAllDead) {
    //         Dolphin.SetObjective(_nextObjective);
    //     }
    // }

}
