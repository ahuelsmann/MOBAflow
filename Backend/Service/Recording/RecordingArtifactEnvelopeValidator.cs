// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Recording;

using Moba.Common.Recording;
using System.Text.Json;

using static RecordingJsonReader;

/// <summary>
/// Checks a recording artifact against the limits of the recording format before it is accepted or written.
/// </summary>
internal static class RecordingArtifactEnvelopeValidator
{
    internal const string EntriesPath = "$.entries";

    internal static RecordingValidationError? ValidateArtifactEnvelope(RecordingArtifact artifact)
    {
        var error = ValidateMetadataEnvelope(artifact.Metadata);
        if (error is not null) return error;

        error = ValidateApplicationEnvelope(artifact.SourceApplicationVersion);
        if (error is not null) return error;

        error = ValidateProjectEnvelope(artifact.Project);
        if (error is not null) return error;

        error = ValidateOptionsEnvelope(artifact.Options);
        return error ?? ValidateEntriesEnvelope(artifact);
    }

    internal static RecordingValidationError? ValidateMetadataEnvelope(RecordingSessionMetadata metadata)
    {
        if (metadata.SessionId == Guid.Empty)
        {
            return Error("invalid-id", "$.metadata.sessionId", "Session ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(metadata.Name)
            || metadata.Name != metadata.Name.Trim()
            || metadata.Name.Length > RecordingFormat.MaxSessionNameLength)
        {
            return Error(
                "invalid-session-name",
                "$.metadata.name",
                "Session name must be non-empty, trimmed, and within the format limit.");
        }

        if (metadata.StartedUtc.Offset != TimeSpan.Zero
            || metadata.CompletedUtc is null
            || metadata.CompletedUtc.Value.Offset != TimeSpan.Zero
            || metadata.CompletedUtc < metadata.StartedUtc)
        {
            return Error("invalid-session-time", "$.metadata", "Session timestamps must be valid UTC completion boundaries.");
        }

        return null;
    }

    internal static RecordingValidationError? ValidateApplicationEnvelope(string applicationVersion)
    {
        if (string.IsNullOrWhiteSpace(applicationVersion)
            || applicationVersion.Length > RecordingFormat.MaxApplicationVersionLength)
        {
            return Error(
                "invalid-application-version",
                "$.application.version",
                "Application version is required and within the format limit.");
        }

        return null;
    }

    internal static RecordingValidationError? ValidateProjectEnvelope(RecordingProjectIdentity? project)
    {
        if (project is not null
            && (project.ProjectId == Guid.Empty
                || string.IsNullOrWhiteSpace(project.Name)
                || project.Name.Length > RecordingFormat.MaxProjectNameLength))
        {
            return Error("invalid-project", "$.project", "Project identity is invalid.");
        }

        return null;
    }

    internal static RecordingValidationError? ValidateOptionsEnvelope(RecordingArtifactOptions options)
    {
        if (options.EntryLimit <= 0 || options.EntryLimit > RecordingFormat.DefaultMaxEntries)
        {
            return Error("invalid-entry-limit", "$.options.entryLimit", "Entry limit is outside the supported range.");
        }

        if (options.EstimatedPayloadByteLimit <= 0
            || options.EstimatedPayloadByteLimit > RecordingFormat.DefaultMaxArtifactBytes)
        {
            return Error(
                "invalid-byte-limit",
                "$.options.estimatedPayloadByteLimit",
                "Estimated payload byte limit is outside the supported range.");
        }

        return null;
    }

    internal static RecordingValidationError? ValidateEntriesEnvelope(RecordingArtifact artifact)
    {
        if (artifact.Entries.Length > artifact.Options.EntryLimit)
        {
            return Error("entry-limit", EntriesPath, "Artifact exceeds its entry limit.");
        }

        long previousSequence = 0;
        long previousElapsed = -1;
        long payloadBytes = 0;
        for (var index = 0; index < artifact.Entries.Length; index++)
        {
            var entry = artifact.Entries[index];
            var error = ValidateEntryEnvelope(
                entry,
                index,
                previousSequence,
                previousElapsed,
                ref payloadBytes,
                artifact.Options.EstimatedPayloadByteLimit);
            if (error is not null) return error;

            previousSequence = entry.Sequence;
            previousElapsed = entry.Elapsed.Ticks;
        }

        return null;
    }

    internal static RecordingValidationError? ValidateEntryEnvelope(
        RecordingEntry entry,
        int index,
        long previousSequence,
        long previousElapsed,
        ref long payloadBytes,
        long payloadByteLimit)
    {
        var path = $"{EntriesPath}[{index}]";
        if (entry.Sequence <= previousSequence)
        {
            return Error("invalid-sequence", $"{path}.sequence", "Entry sequences must be positive and strictly increasing.");
        }

        if (entry.TimestampUtc.Offset != TimeSpan.Zero)
        {
            return Error("invalid-timestamp", $"{path}.timestampUtc", "Entry timestamp must be UTC.");
        }

        if (entry.Elapsed.Ticks < 0 || entry.Elapsed.Ticks < previousElapsed)
        {
            return Error("invalid-elapsed", $"{path}.elapsedTicks", "Elapsed offsets must be non-negative and nondecreasing.");
        }

        var error = ValidateEntryStableKeys(entry, path);
        if (error is not null) return error;

        if (entry.CorrelationId == Guid.Empty)
        {
            return Error("invalid-id", $"{path}.correlationId", "Correlation ID cannot be empty.");
        }

        error = ValidateEntryReferences(entry, path);
        if (error is not null) return error;

        error = ValidateEntryPayload(entry, path, out var canonicalPayloadBytes);
        if (error is not null) return error;

        payloadBytes = checked(payloadBytes + canonicalPayloadBytes);
        if (payloadBytes > payloadByteLimit)
        {
            return Error("payload-total-limit", EntriesPath, "Canonical payloads exceed the declared byte limit.");
        }

        return entry.DisplayText.Length > RecordingFormat.MaxDisplayTextLength
            ? Error("display-text-limit", $"{path}.displayText", "Display text exceeds the format limit.")
            : null;
    }

    internal static RecordingValidationError? ValidateEntryStableKeys(RecordingEntry entry, string path)
    {
        foreach (var (value, property, limit) in new[]
                 {
                     (entry.Category, "category", RecordingFormat.MaxKeyLength),
                     (entry.Source, "source", RecordingFormat.MaxKeyLength),
                     (entry.TypeKey, "typeKey", RecordingFormat.MaxTypeKeyLength),
                     (entry.Severity, "severity", RecordingFormat.MaxKeyLength)
                 })
        {
            if (!IsStableKey(value, limit))
            {
                return Error("invalid-key", $"{path}.{property}", "Stable key is empty, too long, or has an invalid shape.");
            }
        }

        return null;
    }

    internal static RecordingValidationError? ValidateEntryReferences(RecordingEntry entry, string path)
    {
        return entry.EntityReferences.Length > RecordingFormat.MaxEntityReferencesPerEntry
               || entry.EntityReferences.Any(reference =>
                   reference.Id == Guid.Empty || !IsStableKey(reference.Kind, RecordingFormat.MaxKeyLength))
            ? Error(
                "invalid-entity-reference",
                $"{path}.entityReferences",
                "Entity reference is invalid or exceeds the supported count.")
            : null;
    }

    internal static RecordingValidationError? ValidateEntryPayload(
        RecordingEntry entry,
        string path,
        out int canonicalPayloadBytes)
    {
        canonicalPayloadBytes = 0;
        if (entry.Payload.ValueKind != JsonValueKind.Object)
        {
            return Error("invalid-payload", $"{path}.payload", "Payload must be an object within the canonical byte limit.");
        }

        canonicalPayloadBytes = RecordingArtifactSerializer.WriteCanonicalPayload(entry.Payload).Length;
        return canonicalPayloadBytes > RecordingFormat.MaxPayloadBytes
            ? Error("invalid-payload", $"{path}.payload", "Payload must be an object within the canonical byte limit.")
            : null;
    }

    internal static RecordingValidationError Error(string code, string path, string message) => new(code, path, message);
}
