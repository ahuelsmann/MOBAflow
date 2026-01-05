// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Geometry;

using Domain;

/// <summary>
/// Extension methods for TrackGeometry to calculate connector transformations.
/// Pure topology-first: Connectors define the transformation chain.
/// </summary>
public static class TrackGeometryExtensions
{
    /// <summary>
    /// Get the local transformation of a connector.
    /// This is the transform FROM segment origin TO connector position.
    /// </summary>
    public static Transform2D GetConnectorTransform(this TrackGeometry geometry, int connectorIndex)
    {
        if (connectorIndex < 0 || connectorIndex >= geometry.Endpoints.Count)
            throw new ArgumentOutOfRangeException(nameof(connectorIndex), $"Connector index {connectorIndex} out of range for {geometry.ArticleCode}");

        var endpoint = geometry.Endpoints[connectorIndex];
        var heading = geometry.EndpointHeadingsDeg[connectorIndex];

        return new Transform2D
        {
            TranslateX = endpoint.X,
            TranslateY = endpoint.Y,
            RotationDegrees = heading
        };
    }

    /// <summary>
    /// Get the inverse transformation of a connector.
    /// This is the transform FROM connector position BACK TO segment origin.
    /// Used when calculating child segment position from parent connector.
    /// </summary>
    public static Transform2D GetInverseConnectorTransform(this TrackGeometry geometry, int connectorIndex)
    {
        var connectorTransform = GetConnectorTransform(geometry, connectorIndex);
        
        var flippedRotation = (connectorTransform.RotationDegrees + 180) % 360;
        
        var rad = flippedRotation * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        
        var invertedX = -connectorTransform.TranslateX * cos + connectorTransform.TranslateY * sin;
        var invertedY = -connectorTransform.TranslateX * sin - connectorTransform.TranslateY * cos;

        return new Transform2D
        {
            TranslateX = invertedX,
            TranslateY = invertedY,
            RotationDegrees = flippedRotation
        };
    }
}
