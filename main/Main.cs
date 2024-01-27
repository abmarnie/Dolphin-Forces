using System;
using System.Collections.Generic;
using Godot;

namespace DolphinForces;

public partial class Main : Node3D {

    public static float ElapsedTimeS => Time.GetTicksMsec() / 1000f;

    private List<Terrain> _terrains = [];

    // private Node3D _terrain = null!;
    // _terrain = GetNode<Node3D>("%UnderwaterTerrain");
    // _terrain.Visible = Dolphin.IsCameraUnderwater;

    public override void _Ready() =>
        _terrains = GetDescendantsOfType<Terrain>();
    // Getting terrains this way is **extremely** stupid.

    public override void _Process(double delta) {
        foreach (var t in _terrains)
            t.Visible = Dolphin.IsCameraUnderwater;
    }

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
