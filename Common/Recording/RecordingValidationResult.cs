// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Recording;

using System.Collections.Immutable;

/// <summary>
/// Describes one precise recording import validation failure.
/// </summary>
public sealed record RecordingValidationError(string Code, string Path, string Message);

/// <summary>
/// Returns either a fully validated artifact or bounded validation errors.
/// </summary>
public sealed class RecordingValidationResult
{
    private RecordingValidationResult(RecordingArtifact? artifact, ImmutableArray<RecordingValidationError> errors)
    {
        Artifact = artifact;
        Errors = errors;
    }

    public bool IsValid => Artifact is not null && Errors.IsEmpty;

    public RecordingArtifact? Artifact { get; }

    public ImmutableArray<RecordingValidationError> Errors { get; }

    public static RecordingValidationResult Success(RecordingArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        return new RecordingValidationResult(artifact, []);
    }

    public static RecordingValidationResult Failure(string code, string path, string message)
    {
        return new RecordingValidationResult(null, [new RecordingValidationError(code, path, message)]);
    }
}