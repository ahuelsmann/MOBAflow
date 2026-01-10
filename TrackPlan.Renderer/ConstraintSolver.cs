// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Renderer;

using Domain;
using Geometry;

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

            _ => parentWorldTransform
        };
    }

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
            return parentWorldTransform;
        }

        var pPoint = parentGeometry.Endpoints[pIdx];
        var pHeading = parentGeometry.EndpointHeadingsDeg[pIdx];

        var cPoint = childGeometry.Endpoints[cIdx];
        var cHeading = childGeometry.EndpointHeadingsDeg[cIdx];

        var childRotation = parentWorldTransform.RotationDegrees + pHeading + 180 - cHeading;
        var (X, Y) = RotatePoint(cPoint.X, cPoint.Y, childRotation);

        var parentWorldConnector = parentWorldTransform.TransformPoint(pPoint.X, pPoint.Y);
        var childWorldX = parentWorldConnector.X - X;
        var childWorldY = parentWorldConnector.Y - Y;

        return new Transform2D
        {
            TranslateX = childWorldX,
            TranslateY = childWorldY,
            RotationDegrees = NormalizeAngle(childRotation)
        };
    }

    private Transform2D CalculateRotationalTransform(
        Transform2D parentWorldTransform,
        TrackGeometry parentGeometry,
        int parentConnectorIndex,
        TrackGeometry childGeometry,
        int childConnectorIndex)
    {
        var rigidTransform = CalculateRigidTransform(
            parentWorldTransform, parentGeometry, parentConnectorIndex,
            childGeometry, childConnectorIndex);

        return new Transform2D
        {
            TranslateX = rigidTransform.TranslateX,
            TranslateY = rigidTransform.TranslateY,
            RotationDegrees = parentWorldTransform.RotationDegrees
        };
    }

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
            return parentWorldTransform;
        }

        var pPoint = parentGeometry.Endpoints[pIdx];
        var pHeading = parentGeometry.EndpointHeadingsDeg[pIdx];

        var cPoint = childGeometry.Endpoints[cIdx];
        var cHeading = childGeometry.EndpointHeadingsDeg[cIdx];

        var childRotation = parentWorldTransform.RotationDegrees + pHeading + branchAngle - cHeading;
        var (X, Y) = RotatePoint(cPoint.X, cPoint.Y, childRotation);
        var parentWorldConnector = parentWorldTransform.TransformPoint(pPoint.X, pPoint.Y);
        var childWorldX = parentWorldConnector.X - X;
        var childWorldY = parentWorldConnector.Y - Y;

        return new Transform2D
        {
            TranslateX = childWorldX,
            TranslateY = childWorldY,
            RotationDegrees = NormalizeAngle(childRotation)
        };
    }

    private static (double X, double Y) RotatePoint(double x, double y, double degrees)
    {
        var rad = degrees * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);

        return (
            (x * cos) - (y * sin),
            (x * sin) + (y * cos)
        );
    }

    private static double NormalizeAngle(double degrees)
    {
        var result = degrees % 360.0;
        if (result < 0) result += 360.0;
        return result;
    }
}
