// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Recording;

using Moba.Common.Recording;
using System.Text;
using System.Text.Json;

/// <summary>
/// Checks recorder requests and captured projections against the limits of the recording format.
/// </summary>
internal static class RecordingRequestValidator
{
    /// <summary>Returns the failure for an invalid start request, or <see langword="null"/> when it is valid.</summary>
    public static RecordingOperationResult? ValidateStartRequest(RecordingSessionStartRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > RecordingFormat.MaxSessionNameLength)
        {
            return RecordingOperationResult.Failure(
                RecordingFailureCode.InvalidRequest,
                $"Session name must contain between 1 and {RecordingFormat.MaxSessionNameLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceApplicationVersion) ||
            request.SourceApplicationVersion.Trim().Length > RecordingFormat.MaxApplicationVersionLength)
        {
            return RecordingOperationResult.Failure(
                RecordingFailureCode.InvalidRequest,
                $"Application version must contain between 1 and {RecordingFormat.MaxApplicationVersionLength} characters.");
        }

        if (request.Project is not null &&
            (request.Project.ProjectId == Guid.Empty ||
             string.IsNullOrWhiteSpace(request.Project.Name) ||
             request.Project.Name.Length > RecordingFormat.MaxProjectNameLength))
        {
            return RecordingOperationResult.Failure(RecordingFailureCode.InvalidRequest, "Project identity is invalid.");
        }

        return null;
    }

    /// <summary>
    /// Returns the estimated UTF-8 payload size of a valid projection, or -1 when the projection breaks a format limit.
    /// </summary>
    public static int ValidateProjection(RecordingEntryProjection projection)
    {
        if (!IsValidKey(projection.Category) ||
            !IsValidKey(projection.Source) ||
            !IsValidKey(projection.Severity) ||
            string.IsNullOrWhiteSpace(projection.TypeKey) ||
            projection.TypeKey.Length > RecordingFormat.MaxTypeKeyLength ||
            projection.DisplayText.Length > RecordingFormat.MaxDisplayTextLength ||
            projection.EntityReferences.Length > RecordingFormat.MaxEntityReferencesPerEntry ||
            projection.EntityReferences.Any(reference =>
                string.IsNullOrWhiteSpace(reference.Kind) ||
                reference.Kind.Length > RecordingFormat.MaxKeyLength ||
                reference.Id == Guid.Empty))
        {
            return -1;
        }

        if (projection.Payload.ValueKind == JsonValueKind.Undefined) return -1;
        var payloadBytes = Encoding.UTF8.GetByteCount(projection.Payload.GetRawText());
        return payloadBytes <= RecordingFormat.MaxPayloadBytes ? payloadBytes : -1;
    }

    private static bool IsValidKey(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= RecordingFormat.MaxKeyLength;
}
