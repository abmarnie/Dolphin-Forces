using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Godot;

namespace DolphinForces;

/// <summary> Static helper extension methods for Godot types. </summary>
public static class GodotExtensions {

    #region CharacterBody3D
    public static Vector3 LocalVelocity(this CharacterBody3D @this) =>
        @this.Transform.Basis.Transposed() * @this.Velocity;
    #endregion CharacterBody3D

    #region Node
    /// <summary> Asserts a condition on a Godot property using reflection. Pass a lambda
    /// for the property, it's expected value, and a comparison function. </summary>
    [Conditional("DEBUG")]
    public static void AssertCond<T>(this Node @this, Expression<Func<T>> propertyAsLambda,
        T comparedValue, Func<T, T, bool> condition) {
        var memberExpression = (MemberExpression)propertyAsLambda.Body;
        var propName = memberExpression.Member.Name;
        var actualValue = propertyAsLambda.Compile().Invoke();
        var messageStart = @this.Owner == @this ? "" : $"{@this.Owner.Name}'s ";
        Debug.Assert(
            condition: condition(actualValue, comparedValue),
            message: messageStart + $"({@this.Name}: {propName}) value was {actualValue},"
                + " which failed the required condition."
        );
    }

    /// <summary> Asserts a equality on a Godot property using reflection. Pass a 
    /// lambda for the property and it's expected value. </summary>
    [Conditional("DEBUG")]
    public static void AssertEq<T>(this Node @this, Expression<Func<T>> propertyExpression, T expectedValue) {
        var memberExpression = (MemberExpression)propertyExpression.Body;
        var propName = memberExpression.Member.Name;
        var actualValue = propertyExpression.Compile().Invoke();
        var messageStart = @this.Owner == @this ? "" : $"{@this.Owner.Name}'s ";
        Debug.Assert(
            condition: EqualityComparer<T>.Default.Equals(actualValue, expectedValue),
            message: messageStart + $"({@this.Name}: {propName}) was {actualValue},"
                + $" which failed to match expected value {expectedValue}."
        );
    }

    /// <summary> Returns true if a descendant of a given type exists. </summary>
    public static bool HasDescendantOfType<T>(this Node @this) =>
        @this.GetChildren().Any(child => child is T || child.HasDescendantOfType<T>());

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
    #endregion Node

    #region Node3D
    /// <summary> Rotates this Node3D using <see cref="Transform3D.LookingAt"/> 
    /// at the given <paramref name="lookTarget"/> by interpolating with the 
    /// <paramref name="weight"/>. </summary>
    public static void LookAtInterpolate( // TODO Make this pure.
        this Node3D @this, Vector3 lookTarget, float weight, out float rotAmount
    ) {
        var originalForward = -@this.Basis.Z;
        var tempTransform = @this.Transform.LookingAt(lookTarget, Vector3.Up);
        @this.Transform = @this.Transform.InterpolateWith(tempTransform, weight);
        var newForward = -@this.Basis.Z;
        rotAmount = (float)Mathf.Acos(originalForward.Normalized().Dot(newForward.Normalized()));
    }
    #endregion Node3D

    #region Vector2
    /// <summary> Returns the same vector, but with clamped magnitude. </summary>
    public static Vector2 ClampMagnitude(this Vector2 @this, float maxLength) =>
        (@this, maxLength, @this.Length()) switch {
            (_, var max, _) when max < 0 => Vector2.Zero,
            (_, var max, var length) when length > max => @this * (max / length),
            _ => @this
        };
    #endregion Vector2

    #region Vector3
    /// <summary> Wraps Godot's built-in <see cref="Tween.InterpolateValue"/> 
    /// static method. </summary>
    public static Vector3 InterpolateValue(this Vector3 @this, Vector3 target, float elapsedTime, float duration,
        Tween.TransitionType transType = Tween.TransitionType.Linear, Tween.EaseType easeType = Tween.EaseType.InOut) =>
            new((float)Tween.InterpolateValue(@this.X, target.X - @this.X, elapsedTime, duration, transType, easeType),
            (float)Tween.InterpolateValue(@this.Y, target.Y - @this.Y, elapsedTime, duration, transType, easeType),
            (float)Tween.InterpolateValue(@this.Z, target.Z - @this.Z, elapsedTime, duration, transType, easeType));

    /// <summary> Returns the same vector, but with clamped magnitude. </summary>
    public static Vector3 ClampMagnitude(this Vector3 @this, float maxLength) =>
        (@this, maxLength, @this.Length()) switch {
            (_, var max, _) when max < 0 => Vector3.Zero,
            (_, var max, var length) when length > max => @this * (max / length),
            _ => @this
        };
    #endregion Vector3

}