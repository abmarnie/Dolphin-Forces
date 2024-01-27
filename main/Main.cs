using System;
using Godot;

namespace DolphinForces;

public partial class Main : Node3D {

    [Export] private Node3D _terrain = null!;

    public override void _Process(double delta) => _terrain.Visible = Dolphin.IsCameraUnderwater;

}
