// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Geometry;

using Moba.TrackLibrary.Base.TrackSystem;
using Moba.TrackPlan.Graph;

// NOTE: TopologyResolver is in TrackPlan.Renderer - we need it here but can't circular reference
// Solution: Pass TopologyResolver as interface/abstract or move to a shared layer
// For now, this file will be refactored in Phase 3 when we separate concerns better

/// <summary>
/// Represents a calculated position for a node in the track plan.
/// </summary>
public sealed record CalculatedNodePosition(
    Guid NodeId,
    double X,
    double Y,
    double ExitAngleDeg);

/// <summary>
/// Validation error in geometry calculations.
/// </summary>
public sealed record GeometryValidationError(
    Guid EdgeId,
    string TemplateId,
    ValidationErrorType ErrorType,
    string Message);

/// <summary>
/// Types of geometry validation errors.
/// </summary>
public enum ValidationErrorType
{
    TemplateNotFound,
    MissingConnection,
    InvalidAngle,
    PositionConflict,
    PortMismatch
}

/// <summary>
/// Placeholder for GeometryCalculationEngine.
/// Full implementation requires cross-layer reference resolution.
/// </summary>
public sealed class GeometryCalculationEngine
{
    // This class will be properly implemented in Phase 3
    // when we decouple TopologyResolver from Renderer
}
