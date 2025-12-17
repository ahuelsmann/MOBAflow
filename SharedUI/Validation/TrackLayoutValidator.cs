// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Validation;

using Domain.TrackPlan;

/// <summary>
/// Validates track layout rules, including unique InPort assignment.
/// </summary>
public static class TrackLayoutValidator
{
    /// <summary>
    /// Validates that all InPort values are unique within the layout.
    /// </summary>
    /// <param name="segments">All track segments to validate.</param>
    /// <returns>List of validation errors (empty if valid).</returns>
    public static List<ValidationError> ValidateUniqueInPorts(IEnumerable<TrackSegment> segments)
    {
        var errors = new List<ValidationError>();
        var inPortGroups = segments
            .Where(s => s.AssignedInPort.HasValue)
            .GroupBy(s => s.AssignedInPort!.Value)
            .Where(g => g.Count() > 1);

        foreach (var group in inPortGroups)
        {
            var duplicateSegments = group.Select(s => s.ArticleCode).ToList();
            errors.Add(new ValidationError
            {
                ErrorType = ValidationErrorType.DuplicateInPort,
                Message = $"InPort {group.Key} is assigned to multiple segments: {string.Join(", ", duplicateSegments)}",
                AffectedSegmentIds = group.Select(s => s.Id).ToList()
            });
        }

        return errors;
    }

    /// <summary>
    /// Checks if a specific InPort value is available (not already used).
    /// </summary>
    /// <param name="segments">All track segments.</param>
    /// <param name="inPort">The InPort value to check.</param>
    /// <param name="excludeSegmentId">Segment ID to exclude from check (for updates).</param>
    /// <returns>True if the InPort is available.</returns>
    public static bool IsInPortAvailable(IEnumerable<TrackSegment> segments, uint inPort, string? excludeSegmentId = null)
    {
        return !segments.Any(s => 
            s.AssignedInPort == inPort && 
            s.Id != excludeSegmentId);
    }

    /// <summary>
    /// Gets the segment that currently has the specified InPort assigned.
    /// </summary>
    /// <param name="segments">All track segments.</param>
    /// <param name="inPort">The InPort value to find.</param>
    /// <returns>The segment with this InPort, or null if not found.</returns>
    public static TrackSegment? GetSegmentByInPort(IEnumerable<TrackSegment> segments, uint inPort)
    {
        return segments.FirstOrDefault(s => s.AssignedInPort == inPort);
    }
}

/// <summary>
/// Represents a validation error in the track layout.
/// </summary>
public class ValidationError
{
    public ValidationErrorType ErrorType { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> AffectedSegmentIds { get; set; } = [];
}

/// <summary>
/// Types of validation errors.
/// </summary>
public enum ValidationErrorType
{
    DuplicateInPort,
    InvalidInPortRange,
    DisconnectedSegment,
    OverlappingSegments
}
