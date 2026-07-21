// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

using Common.Recording;

/// <summary>
/// Describes the result of exporting a recording artifact through a platform file picker.
/// </summary>
public sealed record RecordingFileExportResult(
    bool Succeeded,
    bool WasCancelled,
    string? Path,
    string? ErrorMessage);

/// <summary>
/// Describes the result of importing and validating a recording artifact through a platform file picker.
/// </summary>
public sealed record RecordingFileImportResult(
    bool Succeeded,
    bool WasCancelled,
    string? Path,
    RecordingArtifact? Artifact,
    string? ErrorMessage);

/// <summary>
/// Provides platform-neutral recording import and export operations to SharedUI ViewModels.
/// </summary>
public interface IRecordingFileService
{
    /// <summary>Exports a completed artifact using a platform save picker and an atomic write.</summary>
    Task<RecordingFileExportResult> ExportAsync(
        RecordingArtifact artifact,
        CancellationToken cancellationToken = default);

    /// <summary>Imports and validates one recording artifact using a platform open picker.</summary>
    Task<RecordingFileImportResult> ImportAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Supplies stable application and project identity for a newly started recording session.
/// </summary>
public interface IRecordingContextProvider
{
    /// <summary>Gets the source application version written into new artifacts.</summary>
    string SourceApplicationVersion { get; }

    /// <summary>Gets the currently selected project identity, or <see langword="null"/> when none is active.</summary>
    RecordingProjectIdentity? GetProjectIdentity();
}