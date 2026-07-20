// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Recording;

using Moba.Common.Recording;
using System.Collections.Frozen;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

public sealed partial class RecordingArtifactSerializer
{
    private static readonly Regex StableKeyPattern = new("^[a-z0-9]+(?:[.-][a-z0-9]+)*$", RegexOptions.CultureInvariant);
    private static readonly FrozenSet<string> RootProperties = CreatePropertySet(
        "format",
        "formatVersion",
        "metadata",
        "application",
        "project",
        "options",
        "entries",
        "summary",
        "integrity");
    private static readonly FrozenSet<string> MetadataProperties = CreatePropertySet("sessionId", "name", "startedUtc", "completedUtc");
    private static readonly FrozenSet<string> ApplicationProperties = CreatePropertySet("version");
    private static readonly FrozenSet<string> ProjectProperties = CreatePropertySet("id", "name");
    private static readonly FrozenSet<string> OptionsProperties = CreatePropertySet("entryLimit", "estimatedPayloadByteLimit");
    private static readonly FrozenSet<string> EntryProperties = CreatePropertySet(
        "sequence",
        "timestampUtc",
        "elapsedTicks",
        "category",
        "source",
        "typeKey",
        "severity",
        "correlationId",
        "entityReferences",
        "payload",
        "displayText",
        "replayApplicability");
    private static readonly FrozenSet<string> EntityReferenceProperties = CreatePropertySet("kind", "id");
    private static readonly FrozenSet<string> SummaryProperties = CreatePropertySet(
        "entryCount",
        "replayApplicableEntryCount",
        "displayOnlyEntryCount",
        "markerCount",
        "noteCount",
        "firstSequence",
        "lastSequence",
        "durationTicks");
    private static readonly FrozenSet<string> IntegrityProperties = CreatePropertySet("entriesSha256");

    public RecordingValidationResult Import(ReadOnlyMemory<byte> utf8Json)
    {
        if (utf8Json.IsEmpty)
        {
            return Failure("empty", "$", "Recording content is empty.");
        }

        if (utf8Json.Length > _importLimits.MaxArtifactBytes)
        {
            return Failure("artifact-too-large", "$", $"Recording exceeds the {_importLimits.MaxArtifactBytes}-byte import limit.");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(
                utf8Json,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = RecordingFormat.MaxJsonDepth
                });
        }
        catch (JsonException ex)
        {
            return Failure("invalid-json", "$", $"Recording JSON is invalid: {ex.Message}");
        }

        using (document)
        {
            try
            {
                return ImportDocument(document.RootElement);
            }
            catch (InvalidRecordingException ex)
            {
                return Failure(ex.Code, ex.Path, ex.Message);
            }
            catch (Exception ex) when (ex is FormatException or InvalidOperationException or OverflowException)
            {
                return Failure("invalid-value", "$", $"Recording contains an invalid value: {ex.Message}");
            }
        }
    }

    private RecordingValidationResult ImportDocument(JsonElement root)
    {
        RequireKind(root, JsonValueKind.Object, "$", "Recording root must be an object.");
        RejectDuplicateProperties(root, "$", recursive: true);
        RequireOnlyProperties(root, "$", RootProperties);

        RequireString(root, "format", "$", RecordingFormat.MaxKeyLength, out var format);
        if (format != RecordingFormat.Identifier)
        {
            throw Invalid("unsupported-format", "$.format", $"Expected '{RecordingFormat.Identifier}'.");
        }

        RequireString(root, "formatVersion", "$", RecordingFormat.MaxKeyLength, out var version);
        if (version != RecordingFormat.Version)
        {
            throw Invalid("unsupported-version", "$.formatVersion", $"Only format version '{RecordingFormat.Version}' is supported.");
        }

        var metadata = ReadMetadata(RequireProperty(root, "metadata", "$"));
        var applicationVersion = ReadApplication(RequireProperty(root, "application", "$"));
        var project = ReadProject(RequireProperty(root, "project", "$"));
        var options = ReadOptions(RequireProperty(root, "options", "$"));

        var entriesElement = RequireProperty(root, "entries", "$");
        RequireKind(entriesElement, JsonValueKind.Array, "$.entries", "Entries must be an array.");
        var entryCount = entriesElement.GetArrayLength();
        if (entryCount > options.EntryLimit || entryCount > _importLimits.MaxEntries)
        {
            throw Invalid("entry-limit", "$.entries", "Recording exceeds its declared or supported entry limit.");
        }

        var entries = new List<RecordingEntry>(entryCount);
        long previousSequence = 0;
        long previousElapsedTicks = -1;
        long payloadBytes = 0;
        var index = 0;
        foreach (var entryElement in entriesElement.EnumerateArray())
        {
            var entry = ReadEntry(entryElement, index, previousSequence, previousElapsedTicks, out var canonicalPayloadBytes);
            entries.Add(entry);
            previousSequence = entry.Sequence;
            previousElapsedTicks = entry.Elapsed.Ticks;
            payloadBytes = checked(payloadBytes + canonicalPayloadBytes);
            if (payloadBytes > options.EstimatedPayloadByteLimit)
            {
                throw Invalid("payload-total-limit", "$.entries", "Canonical payloads exceed the declared byte limit.");
            }

            index++;
        }

        var computedSummary = RecordingSummary.Create(entries);
        ValidateSummary(RequireProperty(root, "summary", "$"), computedSummary);
        var declaredHash = ReadIntegrity(RequireProperty(root, "integrity", "$"));
        var computedHash = Convert.ToHexStringLower(SHA256.HashData(WriteEntries(entries, indented: false)));
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(declaredHash), Convert.FromHexString(computedHash)))
        {
            throw Invalid("integrity-mismatch", "$.integrity.entriesSha256", "Entry integrity hash does not match canonical entries.");
        }

        var artifact = new RecordingArtifact(metadata, applicationVersion, project, options, entries, declaredHash);
        var envelopeError = ValidateArtifactEnvelope(artifact);
        if (envelopeError is not null)
        {
            return RecordingValidationResult.Failure(envelopeError.Code, envelopeError.Path, envelopeError.Message);
        }

        return RecordingValidationResult.Success(artifact);
    }

    private RecordingEntry ReadEntry(
        JsonElement element,
        int index,
        long previousSequence,
        long previousElapsedTicks,
        out int canonicalPayloadBytes)
    {
        var path = $"$.entries[{index}]";
        RequireKind(element, JsonValueKind.Object, path, "Entry must be an object.");
        RequireOnlyProperties(element, path, EntryProperties);

        var sequence = RequireInt64(element, "sequence", path);
        if (sequence <= previousSequence)
        {
            throw Invalid("invalid-sequence", $"{path}.sequence", "Entry sequences must be positive and strictly increasing.");
        }

        var timestamp = RequireTimestamp(element, "timestampUtc", path);
        var elapsedTicks = RequireInt64(element, "elapsedTicks", path);
        if (elapsedTicks < 0 || elapsedTicks < previousElapsedTicks)
        {
            throw Invalid("invalid-elapsed", $"{path}.elapsedTicks", "Elapsed offsets must be non-negative and nondecreasing.");
        }

        var category = RequireStableKey(element, "category", path, RecordingFormat.MaxKeyLength);
        var source = RequireStableKey(element, "source", path, RecordingFormat.MaxKeyLength);
        var typeKey = RequireStableKey(element, "typeKey", path, RecordingFormat.MaxTypeKeyLength);
        var severity = RequireStableKey(element, "severity", path, RecordingFormat.MaxKeyLength);
        var correlationId = ReadNullableGuid(RequireProperty(element, "correlationId", path), $"{path}.correlationId");
        var entityReferences = ReadEntityReferences(RequireProperty(element, "entityReferences", path), path);
        var payload = RequireProperty(element, "payload", path);
        RequireKind(payload, JsonValueKind.Object, $"{path}.payload", "Payload must be an object.");
        canonicalPayloadBytes = WriteCanonicalPayload(payload).Length;
        if (canonicalPayloadBytes > RecordingFormat.MaxPayloadBytes)
        {
            throw Invalid("payload-too-large", $"{path}.payload", $"Canonical payload exceeds {RecordingFormat.MaxPayloadBytes} bytes.");
        }

        RequireString(element, "displayText", path, RecordingFormat.MaxDisplayTextLength, out var displayText);
        var declaredApplicability = ReadReplayApplicability(RequireProperty(element, "replayApplicability", path), path);
        var effectiveApplicability = RecordingReplayApplicability.DisplayOnly;
        if (_payloadValidators.TryGetValue(typeKey, out var validator))
        {
            RecordingPayloadValidationResult payloadResult;
            try
            {
                payloadResult = validator.Validate(payload);
            }
            catch (Exception)
            {
                throw Invalid("invalid-known-payload", $"{path}.payload", "Known payload validation failed.");
            }

            if (!payloadResult.IsValid)
            {
                throw Invalid("invalid-known-payload", $"{path}.payload", payloadResult.ErrorMessage ?? "Known payload is invalid.");
            }

            if (declaredApplicability != validator.ReplayApplicability)
            {
                throw Invalid(
                    "invalid-replay-applicability",
                    $"{path}.replayApplicability",
                    "Known type replay applicability does not match its allow-list registration.");
            }

            effectiveApplicability = validator.ReplayApplicability;
        }

        return new RecordingEntry(
            sequence,
            timestamp,
            TimeSpan.FromTicks(elapsedTicks),
            category,
            source,
            typeKey,
            severity,
            correlationId,
            entityReferences,
            payload,
            displayText,
            effectiveApplicability);
    }

    private static RecordingSessionMetadata ReadMetadata(JsonElement element)
    {
        const string Path = "$.metadata";
        RequireKind(element, JsonValueKind.Object, Path, "Metadata must be an object.");
        RequireOnlyProperties(element, Path, MetadataProperties);
        var sessionId = RequireGuid(element, "sessionId", Path);
        RequireString(element, "name", Path, RecordingFormat.MaxSessionNameLength, out var name);
        if (string.IsNullOrWhiteSpace(name) || name != name.Trim())
        {
            throw Invalid("invalid-session-name", $"{Path}.name", "Session name must be non-empty and trimmed.");
        }

        var startedUtc = RequireTimestamp(element, "startedUtc", Path);
        var completedUtc = RequireTimestamp(element, "completedUtc", Path);
        if (completedUtc < startedUtc)
        {
            throw Invalid("invalid-session-time", $"{Path}.completedUtc", "Completion time cannot precede start time.");
        }

        return new RecordingSessionMetadata(sessionId, name, startedUtc, completedUtc);
    }

    private static string ReadApplication(JsonElement element)
    {
        const string Path = "$.application";
        RequireKind(element, JsonValueKind.Object, Path, "Application must be an object.");
        RequireOnlyProperties(element, Path, ApplicationProperties);
        RequireString(element, "version", Path, RecordingFormat.MaxApplicationVersionLength, out var version);
        if (string.IsNullOrWhiteSpace(version))
        {
            throw Invalid("invalid-application-version", $"{Path}.version", "Application version is required.");
        }

        return version;
    }

    private static RecordingProjectIdentity? ReadProject(JsonElement element)
    {
        const string Path = "$.project";
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        RequireKind(element, JsonValueKind.Object, Path, "Project must be an object or null.");
        RequireOnlyProperties(element, Path, ProjectProperties);
        var id = RequireGuid(element, "id", Path);
        RequireString(element, "name", Path, RecordingFormat.MaxProjectNameLength, out var name);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw Invalid("invalid-project-name", $"{Path}.name", "Project name is required when project identity is present.");
        }

        return new RecordingProjectIdentity(id, name);
    }

    private static RecordingArtifactOptions ReadOptions(JsonElement element)
    {
        const string Path = "$.options";
        RequireKind(element, JsonValueKind.Object, Path, "Options must be an object.");
        RequireOnlyProperties(element, Path, OptionsProperties);
        var entryLimit = RequireInt32(element, "entryLimit", Path);
        var byteLimit = RequireInt64(element, "estimatedPayloadByteLimit", Path);
        if (entryLimit <= 0 || entryLimit > RecordingFormat.DefaultMaxEntries)
        {
            throw Invalid("invalid-entry-limit", $"{Path}.entryLimit", "Entry limit is outside the supported range.");
        }

        if (byteLimit <= 0 || byteLimit > RecordingFormat.DefaultMaxArtifactBytes)
        {
            throw Invalid("invalid-byte-limit", $"{Path}.estimatedPayloadByteLimit", "Byte limit is outside the supported range.");
        }

        return new RecordingArtifactOptions(entryLimit, byteLimit);
    }

    private static IReadOnlyList<RecordingEntityReference> ReadEntityReferences(JsonElement element, string entryPath)
    {
        var path = $"{entryPath}.entityReferences";
        RequireKind(element, JsonValueKind.Array, path, "Entity references must be an array.");
        if (element.GetArrayLength() > RecordingFormat.MaxEntityReferencesPerEntry)
        {
            throw Invalid("entity-reference-limit", path, "Entry has too many entity references.");
        }

        var references = new List<RecordingEntityReference>(element.GetArrayLength());
        var index = 0;
        foreach (var referenceElement in element.EnumerateArray())
        {
            var referencePath = $"{path}[{index}]";
            RequireKind(referenceElement, JsonValueKind.Object, referencePath, "Entity reference must be an object.");
            RequireOnlyProperties(referenceElement, referencePath, EntityReferenceProperties);
            var kind = RequireStableKey(referenceElement, "kind", referencePath, RecordingFormat.MaxKeyLength);
            var id = RequireGuid(referenceElement, "id", referencePath);
            references.Add(new RecordingEntityReference(kind, id));
            index++;
        }

        return references;
    }

    private static void ValidateSummary(JsonElement element, RecordingSummary expected)
    {
        const string Path = "$.summary";
        RequireKind(element, JsonValueKind.Object, Path, "Summary must be an object.");
        RequireOnlyProperties(element, Path, SummaryProperties);
        var actual = new RecordingSummary(
            RequireInt32(element, "entryCount", Path),
            RequireInt32(element, "replayApplicableEntryCount", Path),
            RequireInt32(element, "displayOnlyEntryCount", Path),
            RequireInt32(element, "markerCount", Path),
            RequireInt32(element, "noteCount", Path),
            RequireInt64(element, "firstSequence", Path),
            RequireInt64(element, "lastSequence", Path),
            RequireInt64(element, "durationTicks", Path));
        if (actual != expected)
        {
            throw Invalid("summary-mismatch", Path, "Declared summary does not match the imported entries.");
        }
    }

    private static string ReadIntegrity(JsonElement element)
    {
        const string Path = "$.integrity";
        RequireKind(element, JsonValueKind.Object, Path, "Integrity must be an object.");
        RequireOnlyProperties(element, Path, IntegrityProperties);
        RequireString(element, "entriesSha256", Path, 64, out var hash);
        if (hash.Length != 64 || hash.Any(character => !Uri.IsHexDigit(character)) || hash != hash.ToLowerInvariant())
        {
            throw Invalid("invalid-integrity", $"{Path}.entriesSha256", "Entry integrity must be a lowercase SHA-256 hexadecimal value.");
        }

        return hash;
    }

    private static RecordingReplayApplicability ReadReplayApplicability(JsonElement element, string entryPath)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw Invalid("invalid-replay-applicability", $"{entryPath}.replayApplicability", "Replay applicability must be a string.");
        }

        return element.GetString() switch
        {
            "displayOnly" => RecordingReplayApplicability.DisplayOnly,
            "replayApplicable" => RecordingReplayApplicability.ReplayApplicable,
            _ => throw Invalid("invalid-replay-applicability", $"{entryPath}.replayApplicability", "Replay applicability is unsupported.")
        };
    }

    private static RecordingValidationError? ValidateArtifactEnvelope(RecordingArtifact artifact)
    {
        if (artifact.Metadata.SessionId == Guid.Empty)
        {
            return Error("invalid-id", "$.metadata.sessionId", "Session ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(artifact.Metadata.Name)
            || artifact.Metadata.Name != artifact.Metadata.Name.Trim()
            || artifact.Metadata.Name.Length > RecordingFormat.MaxSessionNameLength)
        {
            return Error(
                "invalid-session-name",
                "$.metadata.name",
                "Session name must be non-empty, trimmed, and within the format limit.");
        }

        if (artifact.Metadata.StartedUtc.Offset != TimeSpan.Zero
            || artifact.Metadata.CompletedUtc is null
            || artifact.Metadata.CompletedUtc.Value.Offset != TimeSpan.Zero
            || artifact.Metadata.CompletedUtc < artifact.Metadata.StartedUtc)
        {
            return Error("invalid-session-time", "$.metadata", "Session timestamps must be valid UTC completion boundaries.");
        }

        if (string.IsNullOrWhiteSpace(artifact.SourceApplicationVersion)
            || artifact.SourceApplicationVersion.Length > RecordingFormat.MaxApplicationVersionLength)
        {
            return Error(
                "invalid-application-version",
                "$.application.version",
                "Application version is required and within the format limit.");
        }

        if (artifact.Project is not null
            && (artifact.Project.ProjectId == Guid.Empty
                || string.IsNullOrWhiteSpace(artifact.Project.Name)
                || artifact.Project.Name.Length > RecordingFormat.MaxProjectNameLength))
        {
            return Error("invalid-project", "$.project", "Project identity is invalid.");
        }

        if (artifact.Options.EntryLimit <= 0 || artifact.Options.EntryLimit > RecordingFormat.DefaultMaxEntries)
        {
            return Error("invalid-entry-limit", "$.options.entryLimit", "Entry limit is outside the supported range.");
        }

        if (artifact.Options.EstimatedPayloadByteLimit <= 0
            || artifact.Options.EstimatedPayloadByteLimit > RecordingFormat.DefaultMaxArtifactBytes)
        {
            return Error(
                "invalid-byte-limit",
                "$.options.estimatedPayloadByteLimit",
                "Estimated payload byte limit is outside the supported range.");
        }

        if (artifact.Entries.Length > artifact.Options.EntryLimit)
        {
            return Error("entry-limit", "$.entries", "Artifact exceeds its entry limit.");
        }

        long previousSequence = 0;
        long previousElapsed = -1;
        long payloadBytes = 0;
        for (var index = 0; index < artifact.Entries.Length; index++)
        {
            var entry = artifact.Entries[index];
            var path = $"$.entries[{index}]";
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

            if (entry.CorrelationId == Guid.Empty)
            {
                return Error("invalid-id", $"{path}.correlationId", "Correlation ID cannot be empty.");
            }

            if (entry.EntityReferences.Length > RecordingFormat.MaxEntityReferencesPerEntry
                || entry.EntityReferences.Any(reference =>
                    reference.Id == Guid.Empty || !IsStableKey(reference.Kind, RecordingFormat.MaxKeyLength)))
            {
                return Error(
                    "invalid-entity-reference",
                    $"{path}.entityReferences",
                    "Entity reference is invalid or exceeds the supported count.");
            }

            if (entry.Payload.ValueKind != JsonValueKind.Object)
            {
                return Error("invalid-payload", $"{path}.payload", "Payload must be an object within the canonical byte limit.");
            }

            var canonicalPayloadBytes = WriteCanonicalPayload(entry.Payload).Length;
            if (canonicalPayloadBytes > RecordingFormat.MaxPayloadBytes)
            {
                return Error("invalid-payload", $"{path}.payload", "Payload must be an object within the canonical byte limit.");
            }

            payloadBytes = checked(payloadBytes + canonicalPayloadBytes);
            if (payloadBytes > artifact.Options.EstimatedPayloadByteLimit)
            {
                return Error("payload-total-limit", "$.entries", "Canonical payloads exceed the declared byte limit.");
            }

            if (entry.DisplayText.Length > RecordingFormat.MaxDisplayTextLength)
            {
                return Error("display-text-limit", $"{path}.displayText", "Display text exceeds the format limit.");
            }

            previousSequence = entry.Sequence;
            previousElapsed = entry.Elapsed.Ticks;
        }

        return null;
    }

    private static JsonElement RequireProperty(JsonElement element, string propertyName, string parentPath)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            throw Invalid("missing-property", $"{parentPath}.{propertyName}", $"Required property '{propertyName}' is missing.");
        }

        return value;
    }

    private static void RequireString(JsonElement element, string propertyName, string parentPath, int maxLength, out string value)
    {
        var property = RequireProperty(element, propertyName, parentPath);
        if (property.ValueKind != JsonValueKind.String)
        {
            throw Invalid("invalid-string", $"{parentPath}.{propertyName}", $"Property '{propertyName}' must be a string.");
        }

        value = property.GetString()!;
        if (value.Length > maxLength)
        {
            throw Invalid("string-too-long", $"{parentPath}.{propertyName}", $"Property '{propertyName}' exceeds {maxLength} characters.");
        }
    }

    private static string RequireStableKey(JsonElement element, string propertyName, string parentPath, int maxLength)
    {
        RequireString(element, propertyName, parentPath, maxLength, out var value);
        if (!IsStableKey(value, maxLength))
        {
            throw Invalid("invalid-key", $"{parentPath}.{propertyName}", $"Property '{propertyName}' is not a lowercase stable key.");
        }

        return value;
    }

    private static bool IsStableKey(string value, int maxLength)
    {
        return value.Length is > 0 && value.Length <= maxLength && StableKeyPattern.IsMatch(value);
    }

    private static int RequireInt32(JsonElement element, string propertyName, string parentPath)
    {
        var property = RequireProperty(element, propertyName, parentPath);
        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out var value))
        {
            throw Invalid("invalid-number", $"{parentPath}.{propertyName}", $"Property '{propertyName}' must be a 32-bit integer.");
        }

        return value;
    }

    private static long RequireInt64(JsonElement element, string propertyName, string parentPath)
    {
        var property = RequireProperty(element, propertyName, parentPath);
        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt64(out var value))
        {
            throw Invalid("invalid-number", $"{parentPath}.{propertyName}", $"Property '{propertyName}' must be a 64-bit integer.");
        }

        return value;
    }

    private static Guid RequireGuid(JsonElement element, string propertyName, string parentPath)
    {
        var property = RequireProperty(element, propertyName, parentPath);
        return ReadGuid(property, $"{parentPath}.{propertyName}");
    }

    private static Guid ReadGuid(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.String
            || !Guid.TryParseExact(element.GetString(), "D", out var value)
            || value == Guid.Empty)
        {
            throw Invalid("invalid-id", path, "Identifier must be a non-empty canonical GUID.");
        }

        return value;
    }

    private static Guid? ReadNullableGuid(JsonElement element, string path)
    {
        return element.ValueKind == JsonValueKind.Null ? null : ReadGuid(element, path);
    }

    private static DateTimeOffset RequireTimestamp(JsonElement element, string propertyName, string parentPath)
    {
        RequireString(element, propertyName, parentPath, 32, out var value);
        if (!DateTimeOffset.TryParseExact(
                value,
                TimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var timestamp)
            || timestamp.Offset != TimeSpan.Zero)
        {
            throw Invalid("invalid-timestamp", $"{parentPath}.{propertyName}", "Timestamp must be canonical UTC ISO-8601.");
        }

        return timestamp;
    }

    private static void RequireKind(JsonElement element, JsonValueKind kind, string path, string message)
    {
        if (element.ValueKind != kind)
        {
            throw Invalid("invalid-kind", path, message);
        }
    }

    private static void RejectDuplicateProperties(JsonElement element, string path, bool recursive)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw Invalid("duplicate-property", $"{path}.{property.Name}", $"Duplicate property '{property.Name}' is not allowed.");
                }

                if (recursive)
                {
                    RejectDuplicateProperties(property.Value, $"{path}.{property.Name}", recursive: true);
                }
            }
        }
        else if (recursive && element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                RejectDuplicateProperties(item, $"{path}[{index}]", recursive: true);
                index++;
            }
        }
    }

    private static void RequireOnlyProperties(JsonElement element, string path, FrozenSet<string> allowedProperties)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!allowedProperties.Contains(property.Name))
            {
                throw Invalid(
                    "unknown-property",
                    $"{path}.{property.Name}",
                    $"Property '{property.Name}' is not part of recording format 1.0.");
            }
        }
    }

    private static FrozenSet<string> CreatePropertySet(params string[] properties)
    {
        return properties.ToFrozenSet(StringComparer.Ordinal);
    }

    private static RecordingValidationResult Failure(string code, string path, string message)
    {
        return RecordingValidationResult.Failure(code, path, message);
    }

    private static RecordingValidationError Error(string code, string path, string message) => new(code, path, message);

    private static InvalidRecordingException Invalid(string code, string path, string message) => new(code, path, message);

    private sealed class InvalidRecordingException(string code, string path, string message) : Exception(message)
    {
        public string Code { get; } = code;

        public string Path { get; } = path;
    }
}