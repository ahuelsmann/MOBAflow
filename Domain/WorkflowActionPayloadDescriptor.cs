// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

using Enum;

using System.Text.Json;

internal sealed class WorkflowActionPayloadDescriptor(
    ActionType actionType,
    string jsonPropertyName,
    Action<WorkflowAction, JsonElement, JsonSerializerOptions> read,
    Action<Utf8JsonWriter, WorkflowAction, JsonSerializerOptions> write,
    Func<WorkflowAction, bool> hasPayload,
    Action<Utf8JsonWriter, JsonSerializerOptions> writeDefault,
    Action<WorkflowAction, JsonElement> mergeLegacy)
{
    public ActionType ActionType { get; } = actionType;

    public string JsonPropertyName { get; } = jsonPropertyName;

    public void Read(WorkflowAction action, JsonElement element, JsonSerializerOptions options) =>
        read(action, element, options);

    public void Write(Utf8JsonWriter writer, WorkflowAction action, JsonSerializerOptions options) =>
        write(writer, action, options);

    public bool HasPayload(WorkflowAction action) => hasPayload(action);

    public void WriteDefault(Utf8JsonWriter writer, JsonSerializerOptions options) =>
        writeDefault(writer, options);

    public void MergeLegacy(WorkflowAction action, JsonElement legacyParams) =>
        mergeLegacy(action, legacyParams);
}

internal static class WorkflowActionPayloadDescriptors
{
    public static readonly IReadOnlyList<WorkflowActionPayloadDescriptor> All =
    [
        new(
            ActionType.Command,
            "command",
            (action, element, options) => action.Command = JsonSerializer.Deserialize<CommandActionPayload>(element.GetRawText(), options),
            (writer, action, options) => JsonSerializer.Serialize(writer, action.Command!, options),
            action => action.Command != null,
            (writer, options) => JsonSerializer.Serialize(writer, new CommandActionPayload(), options),
            MergeCommandLegacy),
        new(
            ActionType.Audio,
            "audio",
            (action, element, options) => action.Audio = JsonSerializer.Deserialize<AudioActionPayload>(element.GetRawText(), options),
            (writer, action, options) => JsonSerializer.Serialize(writer, action.Audio!, options),
            action => action.Audio != null,
            (writer, options) => JsonSerializer.Serialize(writer, new AudioActionPayload(), options),
            MergeAudioLegacy),
        new(
            ActionType.Announcement,
            "announcement",
            (action, element, options) => action.Announcement = JsonSerializer.Deserialize<AnnouncementActionPayload>(element.GetRawText(), options),
            (writer, action, options) => JsonSerializer.Serialize(writer, action.Announcement!, options),
            action => action.Announcement != null,
            (writer, options) => JsonSerializer.Serialize(writer, new AnnouncementActionPayload(), options),
            MergeAnnouncementLegacy),
        new(
            ActionType.ExecuteScript,
            "powerShell",
            (action, element, options) => action.PowerShell = JsonSerializer.Deserialize<PowerShellActionPayload>(element.GetRawText(), options),
            (writer, action, options) => JsonSerializer.Serialize(writer, action.PowerShell!, options),
            action => action.PowerShell != null,
            (writer, options) => JsonSerializer.Serialize(writer, new PowerShellActionPayload(), options),
            MergePowerShellLegacy),
        new(
            ActionType.SelectSignalAspect,
            "selectSignalAspect",
            (action, element, options) => action.SelectSignalAspect = JsonSerializer.Deserialize<SelectSignalAspectActionPayload>(element.GetRawText(), options),
            (writer, action, options) => JsonSerializer.Serialize(writer, action.SelectSignalAspect!, options),
            action => action.SelectSignalAspect != null,
            (writer, options) => JsonSerializer.Serialize(writer, new SelectSignalAspectActionPayload(), options),
            NoLegacyMerge),
        new(
            ActionType.TrainDestinationDisplay,
            "trainDestinationDisplay",
            (action, element, options) => action.TrainDestinationDisplay = JsonSerializer.Deserialize<TrainDestinationDisplayActionPayload>(element.GetRawText(), options),
            (writer, action, options) => JsonSerializer.Serialize(writer, action.TrainDestinationDisplay!, options),
            action => action.TrainDestinationDisplay != null,
            (writer, options) => JsonSerializer.Serialize(writer, new TrainDestinationDisplayActionPayload(), options),
            MergeTrainDestinationDisplayLegacy)
    ];

    public static WorkflowActionPayloadDescriptor? Find(ActionType actionType) =>
        All.FirstOrDefault(descriptor => descriptor.ActionType == actionType);

    public static WorkflowActionPayloadDescriptor? FindByPropertyName(string propertyName) =>
        All.FirstOrDefault(descriptor => descriptor.JsonPropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

    private static void MergeCommandLegacy(WorkflowAction action, JsonElement legacyParams)
    {
        action.Command ??= new CommandActionPayload();
        if (string.IsNullOrEmpty(action.Command.BytesBase64) &&
            TryGetInsensitive(legacyParams, "Bytes", out var bytesEl) &&
            TryDecodeCommandBytes(bytesEl) is { Length: > 0 } raw)
        {
            action.Command.BytesBase64 = Convert.ToBase64String(raw);
        }

        if (action.Command.Address == null && TryGetInsensitive(legacyParams, "Address", out var addrEl) && addrEl.TryGetInt32(out var addr))
            action.Command.Address = addr;
        if (action.Command.Speed == null && TryGetInsensitive(legacyParams, "Speed", out var speedEl) && speedEl.TryGetInt32(out var speed))
            action.Command.Speed = speed;
        if (action.Command.Direction == null && TryGetInsensitive(legacyParams, "Direction", out var dirEl) && dirEl.ValueKind == JsonValueKind.String)
            action.Command.Direction = dirEl.GetString();
    }

    private static void MergeAudioLegacy(WorkflowAction action, JsonElement legacyParams)
    {
        action.Audio ??= new AudioActionPayload();
        if (!string.IsNullOrEmpty(action.Audio.FilePath))
            return;

        if (TryGetInsensitive(legacyParams, "FilePath", out var fpEl) && fpEl.ValueKind == JsonValueKind.String)
            action.Audio.FilePath = fpEl.GetString();
        else if (TryGetInsensitive(legacyParams, "AudioFile", out var afEl) && afEl.ValueKind == JsonValueKind.String)
            action.Audio.FilePath = afEl.GetString();
    }

    private static void MergeAnnouncementLegacy(WorkflowAction action, JsonElement legacyParams)
    {
        action.Announcement ??= new AnnouncementActionPayload();
        if (action.Announcement.Message == null && TryGetInsensitive(legacyParams, "Message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String)
            action.Announcement.Message = msgEl.GetString();
        if (action.Announcement.VoiceName == null && TryGetInsensitive(legacyParams, "VoiceName", out var voiceEl) && voiceEl.ValueKind == JsonValueKind.String)
            action.Announcement.VoiceName = voiceEl.GetString();
        if (action.Announcement.Rate == null && TryGetInsensitive(legacyParams, "Rate", out var rateEl) && rateEl.TryGetInt32(out var rate))
            action.Announcement.Rate = rate;
    }

    private static void MergePowerShellLegacy(WorkflowAction action, JsonElement legacyParams)
    {
        action.PowerShell ??= new PowerShellActionPayload();
        if (action.PowerShell.ScriptPath == null && TryGetInsensitive(legacyParams, "ScriptPath", out var spEl) && spEl.ValueKind == JsonValueKind.String)
            action.PowerShell.ScriptPath = spEl.GetString();
        if (action.PowerShell.Arguments == null && TryGetInsensitive(legacyParams, "Arguments", out var argEl) && argEl.ValueKind == JsonValueKind.String)
            action.PowerShell.Arguments = argEl.GetString();
    }

    private static void MergeTrainDestinationDisplayLegacy(WorkflowAction action, JsonElement legacyParams)
    {
        action.TrainDestinationDisplay ??= new TrainDestinationDisplayActionPayload();
        if (action.TrainDestinationDisplay.DisplayDeviceId == Guid.Empty &&
            TryGetInsensitive(legacyParams, "DisplayDeviceId", out var deviceEl) &&
            deviceEl.ValueKind == JsonValueKind.String &&
            Guid.TryParse(deviceEl.GetString(), out var deviceId))
        {
            action.TrainDestinationDisplay.DisplayDeviceId = deviceId;
        }

        if (TryGetInsensitive(legacyParams, "ClearBeforeRender", out var clearEl) &&
            clearEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            action.TrainDestinationDisplay.ClearBeforeRender = clearEl.GetBoolean();
        }
    }

    private static void NoLegacyMerge(WorkflowAction action, JsonElement legacyParams)
    {
        _ = action;
        _ = legacyParams;
    }

    private static byte[]? TryDecodeCommandBytes(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => TryDecodeBase64Bytes(el.GetString()),
            JsonValueKind.Array => TryDecodeByteArray(el),
            _ => null
        };
    }

    private static byte[]? TryDecodeBase64Bytes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static byte[]? TryDecodeByteArray(JsonElement el)
    {
        var list = new List<byte>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.TryGetInt32(out var b) && b is >= 0 and <= 255)
                list.Add((byte)b);
        }

        return list.Count > 0 ? list.ToArray() : null;
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
}