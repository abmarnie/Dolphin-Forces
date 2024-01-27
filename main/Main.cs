using Godot;

namespace DolphinForces;

public partial class Main : Node3D {

    private Node3D _terrain = null!;

    public override void _Ready() => _terrain = GetNode<Node3D>("%UnderwaterTerrain");

    public override void _Process(double delta) => _terrain.Visible = Dolphin.IsCameraUnderwater;

}
