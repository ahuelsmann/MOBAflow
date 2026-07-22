// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Recording;

using Moba.Common.Recording;
using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

/// <summary>
/// Writes deterministic recording v1 artifacts and imports them within fixed resource limits.
/// </summary>
public sealed partial class RecordingArtifactSerializer
{
    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";
    private readonly IReadOnlyDictionary<string, IRecordingPayloadValidator> _payloadValidators;
    private readonly RecordingArtifactImportLimits _importLimits;

    public RecordingArtifactSerializer(
        IEnumerable<IRecordingPayloadValidator>? payloadValidators = null,
        RecordingArtifactImportLimits? importLimits = null)
    {
        _payloadValidators = (payloadValidators ?? [])
            .ToDictionary(validator => validator.TypeKey, StringComparer.Ordinal);
        _importLimits = importLimits ?? new RecordingArtifactImportLimits();
        _importLimits.Validate();
    }

    public string Serialize(RecordingArtifact artifact) => Encoding.UTF8.GetString(SerializeToUtf8(artifact));

    public byte[] SerializeToUtf8(RecordingArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ValidateArtifactForSerialization(artifact);

        var canonicalEntries = WriteEntries(artifact.Entries, indented: false);
        var entriesSha256 = Convert.ToHexStringLower(SHA256.HashData(canonicalEntries));
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("format", RecordingFormat.Identifier);
            writer.WriteString("formatVersion", RecordingFormat.Version);
            WriteMetadata(writer, artifact.Metadata);
            writer.WriteStartObject("application");
            writer.WriteString("version", artifact.SourceApplicationVersion);
            writer.WriteEndObject();
            WriteProject(writer, artifact.Project);
            WriteOptions(writer, artifact.Options);
            writer.WritePropertyName("entries");
            WriteEntries(writer, artifact.Entries);
            WriteSummary(writer, artifact.Summary);
            writer.WriteStartObject("integrity");
            writer.WriteString("entriesSha256", entriesSha256);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return NormalizeNewLines(buffer.WrittenSpan);
    }

    private static byte[] WriteEntries(IReadOnlyList<RecordingEntry> entries, bool indented)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = indented }))
        {
            WriteEntries(writer, entries);
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteEntries(Utf8JsonWriter writer, IReadOnlyList<RecordingEntry> entries)
    {
        writer.WriteStartArray();
        foreach (var entry in entries)
        {
            WriteEntry(writer, entry);
        }

        writer.WriteEndArray();
    }

    private static void WriteEntry(Utf8JsonWriter writer, RecordingEntry entry)
    {
        writer.WriteStartObject();
        writer.WriteNumber("sequence", entry.Sequence);
        writer.WriteString("timestampUtc", FormatTimestamp(entry.TimestampUtc));
        writer.WriteNumber("elapsedTicks", entry.Elapsed.Ticks);
        writer.WriteString("category", entry.Category);
        writer.WriteString("source", entry.Source);
        writer.WriteString("typeKey", entry.TypeKey);
        writer.WriteString("severity", entry.Severity);

        if (entry.CorrelationId.HasValue)
        {
            writer.WriteString("correlationId", FormatGuid(entry.CorrelationId.Value));
        }
        else
        {
            writer.WriteNull("correlationId");
        }

        writer.WriteStartArray("entityReferences");
        foreach (var reference in entry.EntityReferences)
        {
            writer.WriteStartObject();
            writer.WriteString("kind", reference.Kind);
            writer.WriteString("id", FormatGuid(reference.Id));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WritePropertyName("payload");
        WriteCanonicalElement(writer, entry.Payload);
        writer.WriteString("displayText", entry.DisplayText);
        writer.WriteString(
            "replayApplicability",
            entry.ReplayApplicability == RecordingReplayApplicability.ReplayApplicable ? "replayApplicable" : "displayOnly");
        writer.WriteEndObject();
    }

    private static void WriteCanonicalElement(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonicalElement(writer, property.Value);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteCanonicalElement(writer, item);
                }

                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                WriteCanonicalNumber(writer, element);
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                throw new InvalidOperationException("Recording payload contains an unsupported JSON value.");
        }
    }

    private static void WriteMetadata(Utf8JsonWriter writer, RecordingSessionMetadata metadata)
    {
        writer.WriteStartObject("metadata");
        writer.WriteString("sessionId", FormatGuid(metadata.SessionId));
        writer.WriteString("name", metadata.Name);
        writer.WriteString("startedUtc", FormatTimestamp(metadata.StartedUtc));
        writer.WriteString("completedUtc", FormatTimestamp(metadata.CompletedUtc!.Value));
        writer.WriteEndObject();
    }

    private static void WriteProject(Utf8JsonWriter writer, RecordingProjectIdentity? project)
    {
        if (project is null)
        {
            writer.WriteNull("project");
            return;
        }

        writer.WriteStartObject("project");
        writer.WriteString("id", FormatGuid(project.ProjectId));
        writer.WriteString("name", project.Name);
        writer.WriteEndObject();
    }

    private static void WriteOptions(Utf8JsonWriter writer, RecordingArtifactOptions options)
    {
        writer.WriteStartObject("options");
        writer.WriteNumber("entryLimit", options.EntryLimit);
        writer.WriteNumber("estimatedPayloadByteLimit", options.EstimatedPayloadByteLimit);
        writer.WriteEndObject();
    }

    private static void WriteSummary(Utf8JsonWriter writer, RecordingSummary summary)
    {
        writer.WriteStartObject("summary");
        writer.WriteNumber("entryCount", summary.EntryCount);
        writer.WriteNumber("replayApplicableEntryCount", summary.ReplayApplicableEntryCount);
        writer.WriteNumber("displayOnlyEntryCount", summary.DisplayOnlyEntryCount);
        writer.WriteNumber("markerCount", summary.MarkerCount);
        writer.WriteNumber("noteCount", summary.NoteCount);
        writer.WriteNumber("firstSequence", summary.FirstSequence);
        writer.WriteNumber("lastSequence", summary.LastSequence);
        writer.WriteNumber("durationTicks", summary.DurationTicks);
        writer.WriteEndObject();
    }

    private static string FormatTimestamp(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture);
    }

    private static string FormatGuid(Guid value) => value.ToString("D", CultureInfo.InvariantCulture).ToLowerInvariant();

    private static byte[] WriteCanonicalPayload(JsonElement payload)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            WriteCanonicalElement(writer, payload);
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteCanonicalNumber(Utf8JsonWriter writer, JsonElement element)
    {
        if (element.TryGetInt64(out var signedInteger))
        {
            writer.WriteNumberValue(signedInteger);
            return;
        }

        if (element.TryGetUInt64(out var unsignedInteger))
        {
            writer.WriteNumberValue(unsignedInteger);
            return;
        }

        if (element.TryGetDecimal(out var decimalValue))
        {
            writer.WriteRawValue(decimalValue.ToString("G29", CultureInfo.InvariantCulture), skipInputValidation: false);
            return;
        }

        writer.WriteNumberValue(element.GetDouble());
    }

    private static byte[] NormalizeNewLines(ReadOnlySpan<byte> utf8Json)
    {
        var json = Encoding.UTF8.GetString(utf8Json);
        return Encoding.UTF8.GetBytes(json.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    private static void ValidateArtifactForSerialization(RecordingArtifact artifact)
    {
        var result = ValidateArtifactEnvelope(artifact);
        if (result is not null)
        {
            throw new ArgumentException($"{result.Path}: {result.Message}", nameof(artifact));
        }
    }
}