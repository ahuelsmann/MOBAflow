// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Domain.TrackPlan;
using Domain.Geometry;
using Renderer;
using Moba.SharedUI.Geometry; // For extension methods
using System.Diagnostics;

/// <summary>
/// Calculates world transforms for track segments based on connection constraints.
/// Track-Graph Architecture: Core component that converts topology → geometry.
/// Pure topology-first: Uses Transform2D matrices instead of coordinate tuples.
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
    /// Pure matrix-based calculation: parent.WorldTransform * parent.ConnectorTransform * child.InverseConnectorTransform
    /// </summary>
    /// <param name="parentWorldTransform">Parent's world transform matrix</param>
    /// <param name="parentGeometry">Parent's geometry definition</param>
    /// <param name="parentConnectorIndex">Which connector on parent</param>
    /// <param name="childGeometry">Child's geometry definition</param>
    /// <param name="childConnectorIndex">Which connector on child</param>
    /// <param name="constraintType">Type of geometric constraint</param>
    /// <param name="parameters">Optional parameters for parametric constraints</param>
    /// <returns>Child's world transform matrix</returns>
    public Transform2D CalculateWorldTransform(
        Transform2D parentWorldTransform,
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
    /// Pure matrix calculation: parent.WorldTransform * parent.ConnectorTransform * child.InverseConnectorTransform
    /// </summary>
    private Transform2D CalculateRigidTransform(
        Transform2D parentWorldTransform,
        TrackGeometry parentGeometry,
        int parentConnectorIndex,
        TrackGeometry childGeometry,
        int childConnectorIndex)
    {
        var pIdx = parentConnectorIndex;
        var cIdx = childConnectorIndex;

        if (pIdx >= parentGeometry.Endpoints.Count || cIdx >= childGeometry.Endpoints.Count)
        {
            Debug.WriteLine($"⚠️ ConstraintSolver: Invalid connector index: parent[{pIdx}] child[{cIdx}]");
            return parentWorldTransform; // Fallback
        }

        var pPoint = parentGeometry.Endpoints[pIdx];
        var pHeading = parentGeometry.EndpointHeadingsDeg[pIdx];

        var cPoint = childGeometry.Endpoints[cIdx];
        var cHeading = childGeometry.EndpointHeadingsDeg[cIdx];

        // Calculate child rotation: parent rotation + parent heading + 180° - child heading
        var childRotation = parentWorldTransform.RotationDegrees + pHeading + 180 - cHeading;

        // Rotate child connector point by child's rotation
        var rotatedChildConnector = RotatePoint(cPoint.X, cPoint.Y, childRotation);

        // Calculate world position: parent world + parent connector - rotated child connector
        var parentWorldConnector = parentWorldTransform.TransformPoint(pPoint.X, pPoint.Y);
        var childWorldX = parentWorldConnector.X - rotatedChildConnector.X;
        var childWorldY = parentWorldConnector.Y - rotatedChildConnector.Y;

        return new Transform2D
        {
            TranslateX = childWorldX,
            TranslateY = childWorldY,
            RotationDegrees = NormalizeAngle(childRotation)
        };
    }

    /// <summary>
    /// Rotational constraint: Position fixed, heading free.
    /// Used for turntables and rotating bridges.
    /// </summary>
    private Transform2D CalculateRotationalTransform(
        Transform2D parentWorldTransform,
        TrackGeometry parentGeometry,
        int parentConnectorIndex,
        TrackGeometry childGeometry,
        int childConnectorIndex)
    {
        // Position aligns like Rigid, but rotation is free (keep parent rotation)
        var rigidTransform = CalculateRigidTransform(
            parentWorldTransform, parentGeometry, parentConnectorIndex,
            childGeometry, childConnectorIndex);

        // Rotation is not constrained - keep parent's rotation
        return new Transform2D
        {
            TranslateX = rigidTransform.TranslateX,
            TranslateY = rigidTransform.TranslateY,
            RotationDegrees = parentWorldTransform.RotationDegrees
        };
    }

    /// <summary>
    /// Parametric constraint: Position and heading depend on parameter (e.g., switch angle).
    /// Used for switch branches.
    /// </summary>
    private Transform2D CalculateParametricTransform(
        Transform2D parentWorldTransform,
        TrackGeometry parentGeometry,
        int parentConnectorIndex,
        TrackGeometry childGeometry,
        int childConnectorIndex,
        Dictionary<string, double>? parameters)
    {
        var branchAngle = parameters?.GetValueOrDefault("branchAngle", 0) ?? 0;

        var pIdx = parentConnectorIndex;
        var cIdx = childConnectorIndex;

        if (pIdx >= parentGeometry.Endpoints.Count || cIdx >= childGeometry.Endpoints.Count)
        {
            Debug.WriteLine($"⚠️ ConstraintSolver: Invalid connector index: parent[{pIdx}] child[{cIdx}]");
            return parentWorldTransform; // Fallback
        }

        var pPoint = parentGeometry.Endpoints[pIdx];
        var pHeading = parentGeometry.EndpointHeadingsDeg[pIdx];

        var cPoint = childGeometry.Endpoints[cIdx];
        var cHeading = childGeometry.EndpointHeadingsDeg[cIdx];

        // Calculate child rotation: parent rotation + parent heading + branchAngle - child heading
        var childRotation = parentWorldTransform.RotationDegrees + pHeading + branchAngle - cHeading;

        // Rotate child connector point by child's rotation
        var rotatedChildConnector = RotatePoint(cPoint.X, cPoint.Y, childRotation);

        // Calculate world position: parent world + parent connector - rotated child connector
        var parentWorldConnector = parentWorldTransform.TransformPoint(pPoint.X, pPoint.Y);
        var childWorldX = parentWorldConnector.X - rotatedChildConnector.X;
        var childWorldY = parentWorldConnector.Y - rotatedChildConnector.Y;

        return new Transform2D
        {
            TranslateX = childWorldX,
            TranslateY = childWorldY,
            RotationDegrees = NormalizeAngle(childRotation)
        };
    }

    /// <summary>
    /// Rotate a point by angle (degrees).
    /// </summary>
    private static (double X, double Y) RotatePoint(double x, double y, double degrees)
    {
        var rad = degrees * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);

        return (
            x * cos - y * sin,
            x * sin + y * cos
        );
    }

    /// <summary>
    /// Normalize angle to [0, 360) range.
    /// </summary>
    private static double NormalizeAngle(double degrees)
    {
        var result = degrees % 360;
        return result < 0 ? result + 360 : result;
    }
}

