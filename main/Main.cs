using System.Collections.Generic;
using Godot;

namespace DolphinForces;

public partial class Main : Node3D {

    private List<Terrain> _terrains = new();
    // private Node3D _terrain = null!;

    public override void _Ready() =>
        _terrains = GetDescendantsOfType<Terrain>();

    // _terrain = GetNode<Node3D>("%UnderwaterTerrain");

    public override void _Process(double delta) {
        foreach (var t in _terrains)
            t.Visible = Dolphin.IsCameraUnderwater;
        // _terrain.Visible = Dolphin.IsCameraUnderwater;
    }

    // This is **extremely** stupid but I'm too lazy to fix.
    public List<T> GetDescendantsOfType<T>() {
        var matches = new List<T>();
        var queue = new Queue<Node>();
        queue.Enqueue(this);
        while (queue.Count > 0) {
            var current = queue.Dequeue();
            if (current is T match)
                matches.Add(match);
            foreach (var child in current.GetChildren())
                queue.Enqueue(child);
        }
        return matches;
    }

}
