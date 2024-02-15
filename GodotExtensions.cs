using System.Collections.Generic;
using Godot;

namespace DolphinForces;

/// <summary> Static helper extension methods for Godot types. </summary>
public static class GodotExtensions {

    /// <summary> Returns the first descendant of a given type using a 
    /// breadth-first search. </summary>
    public static T? GetDescendant<T>(this Node @this) {
        var queue = new Queue<Node>();
        queue.Enqueue(@this);
        while (queue.Count > 0) {
            var current = queue.Dequeue();
            if (current is T match)
                return match;
            foreach (var child in current.GetChildren())
                queue.Enqueue(child);
        }
        return default;
    }

    /// <summary> Returns list of descendants of a given type, ordered using
    /// a breadth-first search. </summary>
    public static List<T> Descendants<T>(this Node @this) {
        var matches = new List<T>();
        var queue = new Queue<Node>();
        queue.Enqueue(@this);
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