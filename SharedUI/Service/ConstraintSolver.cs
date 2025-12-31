// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Domain.TrackPlan;
using Renderer;
using System.Numerics;

/// <summary>
/// Calculates world transforms for track segments based on connection constraints.
/// Track-Graph Architecture: Core component that converts topology → geometry.
/// </summary>
public class ConstraintSolver
{
    private readonly TrackGeometryLibrary _geometryLibrary;

    public ConstraintSolver(TrackGeometryLibrary geometryLibrary)
    {
        _geometryLibrary = geometryLibrary;
    }

    /// <summary>
    /// Calculate world transform for a child segment connected to a parent segment.
    /// </summary>
    /// <param name="parentWorldTransform">Parent's world transform (position + rotation)</param>
    /// <param name="parentGeometry">Parent's geometry definition</param>
    /// <param name="parentConnectorIndex">Which connector on parent</param>
    /// <param name="childGeometry">Child's geometry definition</param>
    /// <param name="childConnectorIndex">Which connector on child</param>
    /// <param name="constraintType">Type of geometric constraint</param>
    /// <param name="parameters">Optional parameters for parametric constraints</param>
    /// <returns>Child's world transform (X, Y, Rotation in degrees)</returns>
    public (double X, double Y, double Rotation) CalculateWorldTransform(
        (double X, double Y, double Rotation) parentWorldTransform,
        TrackGeometry parentGeometry,
        int parentConnectorIndex,
        TrackGeometry childGeometry,
        int childConnectorIndex,
        ConstraintType constraintType,
        Dictionary<string, double>? parameters = null)
    {
        return constraintType switch
        {
            ConstraintType.Rigid => CalculateRigidTransform(
                parentWorldTransform, parentGeometry, parentConnectorIndex,
                childGeometry, childConnectorIndex),
            
            ConstraintType.Rotational => CalculateRotationalTransform(
                parentWorldTransform, parentGeometry, parentConnectorIndex,
                childGeometry, childConnectorIndex),
            
            ConstraintType.Parametric => CalculateParametricTransform(
                parentWorldTransform, parentGeometry, parentConnectorIndex,
                childGeometry, childConnectorIndex, parameters),
            
            _ => parentWorldTransform // Fallback: no constraint
        };
    }

    /// <summary>
    /// Rigid constraint: Exact position + heading alignment (±180°).
    /// Used for standard track connections.
    /// </summary>
    private (double X, double Y, double Rotation) CalculateRigidTransform(
        (double X, double Y, double Rotation) parentWorldTransform,
        TrackGeometry parentGeometry,
        int parentConnectorIndex,
        TrackGeometry childGeometry,
        int childConnectorIndex)
    {
        // 1. Get parent connector in world coordinates
        if (parentConnectorIndex >= parentGeometry.Endpoints.Count)
            throw new ArgumentException($"Invalid parent connector index: {parentConnectorIndex}");

        var parentLocalConnector = parentGeometry.Endpoints[parentConnectorIndex];
        var parentLocalHeading = parentGeometry.EndpointHeadingsDeg[parentConnectorIndex];

        var (parentWorldX, parentWorldY) = RotateAndTranslate(
            parentLocalConnector.X, parentLocalConnector.Y,
            parentWorldTransform.Rotation,
            parentWorldTransform.X, parentWorldTransform.Y);
        
        var parentWorldHeading = parentWorldTransform.Rotation + parentLocalHeading;

        // 2. Child must align opposite (180° flip for mating connectors)
        if (childConnectorIndex >= childGeometry.Endpoints.Count)
            throw new ArgumentException($"Invalid child connector index: {childConnectorIndex}");

        var childLocalConnector = childGeometry.Endpoints[childConnectorIndex];
        var childLocalHeading = childGeometry.EndpointHeadingsDeg[childConnectorIndex];

        // Child's rotation: Align headings with 180° flip
        var childWorldRotation = parentWorldHeading + 180 - childLocalHeading;

        // 3. Calculate child's origin position
        // Child's connector must be at parent's connector position
        var (childConnectorOffsetX, childConnectorOffsetY) = RotateAndTranslate(
            childLocalConnector.X, childLocalConnector.Y,
            childWorldRotation, 0, 0);

        var childX = parentWorldX - childConnectorOffsetX;
        var childY = parentWorldY - childConnectorOffsetY;

        return (childX, childY, NormalizeAngle(childWorldRotation));
    }

    /// <summary>
    /// Rotational constraint: Position fixed, heading free.
    /// Used for turntables and rotating bridges.
    /// </summary>
    private (double X, double Y, double Rotation) CalculateRotationalTransform(
        (double X, double Y, double Rotation) parentWorldTransform,
        TrackGeometry parentGeometry,
        int parentConnectorIndex,
        TrackGeometry childGeometry,
        int childConnectorIndex)
    {
        // Position aligns like Rigid, but rotation is free (use current rotation or 0)
        var (x, y, _) = CalculateRigidTransform(
            parentWorldTransform, parentGeometry, parentConnectorIndex,
            childGeometry, childConnectorIndex);

        // Rotation is not constrained - keep current or default to 0
        return (x, y, 0);
    }

    /// <summary>
    /// Parametric constraint: Position and heading depend on parameter (e.g., switch angle).
    /// Used for switch branches.
    /// </summary>
    private (double X, double Y, double Rotation) CalculateParametricTransform(
        (double X, double Y, double Rotation) parentWorldTransform,
        TrackGeometry parentGeometry,
        int parentConnectorIndex,
        TrackGeometry childGeometry,
        int childConnectorIndex,
        Dictionary<string, double>? parameters)
    {
        // For now: Same as Rigid (TODO: Add parameter-based offset)
        // Future: Extract branch angle from parameters and apply offset
        var branchAngle = parameters?.GetValueOrDefault("BranchAngle", 0) ?? 0;

        var (x, y, rotation) = CalculateRigidTransform(
            parentWorldTransform, parentGeometry, parentConnectorIndex,
            childGeometry, childConnectorIndex);

        // Apply branch angle offset
        return (x, y, rotation + branchAngle);
    }

    /// <summary>
    /// Rotate and translate a point from local to world coordinates.
    /// </summary>
    private static (double X, double Y) RotateAndTranslate(
        double localX, double localY,
        double rotationDeg,
        double translateX, double translateY)
    {
        var rad = rotationDeg * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);

        var rotatedX = localX * cos - localY * sin;
        var rotatedY = localX * sin + localY * cos;

        return (rotatedX + translateX, rotatedY + translateY);
    }

    /// <summary>
    /// Normalize angle to [0, 360) range.
    /// </summary>
    private static double NormalizeAngle(double angleDeg)
    {
        var result = angleDeg % 360;
        if (result < 0)
            result += 360;
        return result;
    }
}
