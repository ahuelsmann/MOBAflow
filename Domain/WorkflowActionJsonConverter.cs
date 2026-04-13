// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

using Enum;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Serializes <see cref="WorkflowAction"/> as a flat JSON object with typed payload properties
/// (<c>command</c>, <c>audio</c>, <c>announcement</c>, <c>powerShell</c>) instead of a loose <c>parameters</c> map.
/// Migrates legacy <c>parameters</c> on read.
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

        if (TryGetPropertyInsensitive(root, "command", out var commandEl) && commandEl.ValueKind == JsonValueKind.Object)
            action.Command = JsonSerializer.Deserialize<CommandActionPayload>(commandEl.GetRawText(), NestedOptions);

        if (TryGetPropertyInsensitive(root, "audio", out var audioEl) && audioEl.ValueKind == JsonValueKind.Object)
            action.Audio = JsonSerializer.Deserialize<AudioActionPayload>(audioEl.GetRawText(), NestedOptions);

        if (TryGetPropertyInsensitive(root, "announcement", out var annEl) && annEl.ValueKind == JsonValueKind.Object)
            action.Announcement = JsonSerializer.Deserialize<AnnouncementActionPayload>(annEl.GetRawText(), NestedOptions);

        if (TryGetPropertyInsensitive(root, "powerShell", out var psEl) && psEl.ValueKind == JsonValueKind.Object)
            action.PowerShell = JsonSerializer.Deserialize<PowerShellActionPayload>(psEl.GetRawText(), NestedOptions);

        if (TryGetPropertyInsensitive(root, "parameters", out var legacyParams) && legacyParams.ValueKind == JsonValueKind.Object)
            MergeLegacyParameters(action, legacyParams);

        return action;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, WorkflowAction? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        writer.WriteString("id", value.Id);
        writer.WriteString("name", value.Name);
        writer.WriteNumber("number", value.Number);
        writer.WriteNumber("type", (int)value.Type);
        writer.WriteNumber("delayAfterMs", value.DelayAfterMs);

        // Always emit the payload object for the declared type so empty editor state round-trips.
        switch (value.Type)
        {
            case ActionType.Command:
                writer.WritePropertyName("command");
                JsonSerializer.Serialize(writer, value.Command ?? new CommandActionPayload(), NestedOptions);
                break;
            case ActionType.Audio:
                writer.WritePropertyName("audio");
                JsonSerializer.Serialize(writer, value.Audio ?? new AudioActionPayload(), NestedOptions);
                break;
            case ActionType.Announcement:
                writer.WritePropertyName("announcement");
                JsonSerializer.Serialize(writer, value.Announcement ?? new AnnouncementActionPayload(), NestedOptions);
                break;
            case ActionType.ExecutePowerShellScript:
                writer.WritePropertyName("powerShell");
                JsonSerializer.Serialize(writer, value.PowerShell ?? new PowerShellActionPayload(), NestedOptions);
                break;
            default:
                if (value.Command != null)
                {
                    writer.WritePropertyName("command");
                    JsonSerializer.Serialize(writer, value.Command, NestedOptions);
                }

                if (value.Audio != null)
                {
                    writer.WritePropertyName("audio");
                    JsonSerializer.Serialize(writer, value.Audio, NestedOptions);
                }

                if (value.Announcement != null)
                {
                    writer.WritePropertyName("announcement");
                    JsonSerializer.Serialize(writer, value.Announcement, NestedOptions);
                }

                if (value.PowerShell != null)
                {
                    writer.WritePropertyName("powerShell");
                    JsonSerializer.Serialize(writer, value.PowerShell, NestedOptions);
                }

                break;
        }

        writer.WriteEndObject();
    }

    private static void MergeLegacyParameters(WorkflowAction action, JsonElement legacyParams)
    {
        switch (action.Type)
        {
            case ActionType.Command:
                action.Command ??= new CommandActionPayload();
                if (string.IsNullOrEmpty(action.Command.BytesBase64) && TryGetInsensitive(legacyParams, "Bytes", out var bytesEl))
                {
                    var raw = TryDecodeCommandBytes(bytesEl);
                    if (raw is { Length: > 0 })
                        action.Command.BytesBase64 = Convert.ToBase64String(raw);
                }

                if (action.Command.Address == null && TryGetInsensitive(legacyParams, "Address", out var addrEl) && addrEl.TryGetInt32(out var addr))
                    action.Command.Address = addr;
                if (action.Command.Speed == null && TryGetInsensitive(legacyParams, "Speed", out var speedEl) && speedEl.TryGetInt32(out var speed))
                    action.Command.Speed = speed;
                if (action.Command.Direction == null && TryGetInsensitive(legacyParams, "Direction", out var dirEl) && dirEl.ValueKind == JsonValueKind.String)
                    action.Command.Direction = dirEl.GetString();
                break;

            case ActionType.Audio:
                action.Audio ??= new AudioActionPayload();
                if (!string.IsNullOrEmpty(action.Audio.FilePath))
                    break;
                if (TryGetInsensitive(legacyParams, "FilePath", out var fpEl) && fpEl.ValueKind == JsonValueKind.String)
                    action.Audio.FilePath = fpEl.GetString();
                else if (TryGetInsensitive(legacyParams, "AudioFile", out var afEl) && afEl.ValueKind == JsonValueKind.String)
                    action.Audio.FilePath = afEl.GetString();
                break;

            case ActionType.Announcement:
                action.Announcement ??= new AnnouncementActionPayload();
                if (action.Announcement.Message == null && TryGetInsensitive(legacyParams, "Message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String)
                    action.Announcement.Message = msgEl.GetString();
                if (action.Announcement.VoiceName == null && TryGetInsensitive(legacyParams, "VoiceName", out var voiceEl) && voiceEl.ValueKind == JsonValueKind.String)
                    action.Announcement.VoiceName = voiceEl.GetString();
                if (action.Announcement.Rate == null && TryGetInsensitive(legacyParams, "Rate", out var rateEl) && rateEl.TryGetInt32(out var rate))
                    action.Announcement.Rate = rate;
                break;

            case ActionType.ExecutePowerShellScript:
                action.PowerShell ??= new PowerShellActionPayload();
                if (action.PowerShell.ScriptPath == null && TryGetInsensitive(legacyParams, "ScriptPath", out var spEl) && spEl.ValueKind == JsonValueKind.String)
                    action.PowerShell.ScriptPath = spEl.GetString();
                if (action.PowerShell.Arguments == null && TryGetInsensitive(legacyParams, "Arguments", out var argEl) && argEl.ValueKind == JsonValueKind.String)
                    action.PowerShell.Arguments = argEl.GetString();
                break;
        }
    }

    private static byte[]? TryDecodeCommandBytes(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.String:
                var s = el.GetString();
                if (string.IsNullOrWhiteSpace(s))
                    return null;
                try
                {
                    return Convert.FromBase64String(s);
                }
                catch (FormatException)
                {
                    return null;
                }

            case JsonValueKind.Array:
                var list = new List<byte>();
                foreach (var item in el.EnumerateArray())
                {
                    if (item.TryGetInt32(out var b) && b is >= 0 and <= 255)
                        list.Add((byte)b);
                }

                return list.Count > 0 ? list.ToArray() : null;

            default:
                return null;
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
        if (el.TryGetUInt32(out var u))
            return u;
        if (el.TryGetInt32(out var i) && i >= 0)
            return (uint)i;
        return 0;
    }

    private static int ReadInt32(JsonElement root, string name)
    {
        if (!TryGetPropertyInsensitive(root, name, out var el))
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
