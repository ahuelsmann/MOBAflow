// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Serializes <see cref="WorkflowAction"/> as a flat JSON object with typed payload properties
/// instead of a loose <c>parameters</c> map.
/// </summary>
public sealed class WorkflowActionJsonConverter : JsonConverter<WorkflowAction>
{
    private static readonly JsonSerializerOptions NestedOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <inheritdoc />
    public override WorkflowAction? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var action = new WorkflowAction
        {
            Id = ReadGuid(root, "id") ?? Guid.NewGuid(),
            Name = ReadString(root, "name") ?? string.Empty,
            Number = ReadUInt32(root, "number"),
            Type = ReadActionType(root),
            DelayAfterMs = ReadInt32(root, "delayAfterMs")
        };

        foreach (var descriptor in WorkflowActionPayloadDescriptors.All)
        {
            if (TryGetPropertyInsensitive(root, descriptor.JsonPropertyName, out var payloadEl) && payloadEl.ValueKind == JsonValueKind.Object)
                descriptor.Read(action, payloadEl, NestedOptions);
        }

        return action;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, WorkflowAction value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("id", value.Id);
        writer.WriteString("name", value.Name);
        writer.WriteNumber("number", value.Number);
        writer.WriteNumber("type", (int)value.Type);
        writer.WriteNumber("delayAfterMs", value.DelayAfterMs);

        WritePayloads(writer, value);

        writer.WriteEndObject();
    }

    private static void WritePayloads(Utf8JsonWriter writer, WorkflowAction action)
    {
        var declaredDescriptor = WorkflowActionPayloadDescriptors.Find(action.Type);
        if (declaredDescriptor != null)
        {
            writer.WritePropertyName(declaredDescriptor.JsonPropertyName);
            if (declaredDescriptor.HasPayload(action))
                declaredDescriptor.Write(writer, action, NestedOptions);
            else
                declaredDescriptor.WriteDefault(writer, NestedOptions);
            return;
        }

        foreach (var descriptor in WorkflowActionPayloadDescriptors.All)
        {
            if (!descriptor.HasPayload(action))
                continue;

            writer.WritePropertyName(descriptor.JsonPropertyName);
            descriptor.Write(writer, action, NestedOptions);
        }
    }

    private static bool TryGetInsensitive(JsonElement obj, string name, out JsonElement value)
    {
        foreach (var p in obj.EnumerateObject())
        {
            if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = p.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetPropertyInsensitive(JsonElement obj, string name, out JsonElement value) =>
        TryGetInsensitive(obj, name, out value);

    private static Guid? ReadGuid(JsonElement root, string name)
    {
        if (!TryGetPropertyInsensitive(root, name, out var el))
            return null;
        if (el.ValueKind == JsonValueKind.String && Guid.TryParse(el.GetString(), out var g))
            return g;
        return null;
    }

    private static string? ReadString(JsonElement root, string name)
    {
        if (!TryGetPropertyInsensitive(root, name, out var el) || el.ValueKind != JsonValueKind.String)
            return null;
        return el.GetString();
    }

    private static uint ReadUInt32(JsonElement root, string name)
    {
        if (!TryGetPropertyInsensitive(root, name, out var el))
            return 0;
        return el.TryGetUInt32(out var value) ? value : 0;
    }

    private static int ReadInt32(JsonElement root, string name)
    {
        if (!TryGetPropertyInsensitive(root, name, out var el) || el.ValueKind != JsonValueKind.Number)
            return 0;
        return el.TryGetInt32(out var i) ? i : 0;
    }

    private static ActionType ReadActionType(JsonElement root)
    {
        if (!TryGetPropertyInsensitive(root, "type", out var el))
            return ActionType.Command;

        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n))
            return (ActionType)n;

        if (el.ValueKind == JsonValueKind.String)
        {
            var s = el.GetString();
            if (!string.IsNullOrEmpty(s) && System.Enum.TryParse(s, ignoreCase: true, out ActionType parsed))
                return parsed;
        }

        return ActionType.Command;
    }
}
