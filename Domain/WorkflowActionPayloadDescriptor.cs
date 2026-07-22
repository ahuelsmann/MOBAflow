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
    Action<Utf8JsonWriter, JsonSerializerOptions> writeDefault)
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
            (writer, options) => JsonSerializer.Serialize(writer, new CommandActionPayload(), options)),
        new(
            ActionType.Audio,
            "audio",
            (action, element, options) => action.Audio = JsonSerializer.Deserialize<AudioActionPayload>(element.GetRawText(), options),
            (writer, action, options) => JsonSerializer.Serialize(writer, action.Audio!, options),
            action => action.Audio != null,
            (writer, options) => JsonSerializer.Serialize(writer, new AudioActionPayload(), options)),
        new(
            ActionType.Announcement,
            "announcement",
            (action, element, options) => action.Announcement = JsonSerializer.Deserialize<AnnouncementActionPayload>(element.GetRawText(), options),
            (writer, action, options) => JsonSerializer.Serialize(writer, action.Announcement!, options),
            action => action.Announcement != null,
            (writer, options) => JsonSerializer.Serialize(writer, new AnnouncementActionPayload(), options)),
        new(
            ActionType.ExecuteScript,
            "powerShell",
            (action, element, options) => action.PowerShell = JsonSerializer.Deserialize<PowerShellActionPayload>(element.GetRawText(), options),
            (writer, action, options) => JsonSerializer.Serialize(writer, action.PowerShell!, options),
            action => action.PowerShell != null,
            (writer, options) => JsonSerializer.Serialize(writer, new PowerShellActionPayload(), options)),
        new(
            ActionType.SelectSignalAspect,
            "selectSignalAspect",
            (action, element, options) => action.SelectSignalAspect = JsonSerializer.Deserialize<SelectSignalAspectActionPayload>(element.GetRawText(), options),
            (writer, action, options) => JsonSerializer.Serialize(writer, action.SelectSignalAspect!, options),
            action => action.SelectSignalAspect != null,
            (writer, options) => JsonSerializer.Serialize(writer, new SelectSignalAspectActionPayload(), options)),
        new(
            ActionType.TrainDestinationDisplay,
            "trainDestinationDisplay",
            (action, element, options) => action.TrainDestinationDisplay = JsonSerializer.Deserialize<TrainDestinationDisplayActionPayload>(element.GetRawText(), options),
            (writer, action, options) => JsonSerializer.Serialize(writer, action.TrainDestinationDisplay!, options),
            action => action.TrainDestinationDisplay != null,
            (writer, options) => JsonSerializer.Serialize(writer, new TrainDestinationDisplayActionPayload(), options)),
        new(
            ActionType.ChangeJourneyStop,
            "changeJourneyStop",
            (action, element, options) => action.ChangeJourneyStop = JsonSerializer.Deserialize<ChangeJourneyStopActionPayload>(element.GetRawText(), options),
            (writer, action, options) => JsonSerializer.Serialize(writer, action.ChangeJourneyStop!, options),
            action => action.ChangeJourneyStop != null,
            (writer, options) => JsonSerializer.Serialize(writer, new ChangeJourneyStopActionPayload(), options))
    ];

    public static WorkflowActionPayloadDescriptor? Find(ActionType actionType) =>
        All.FirstOrDefault(descriptor => descriptor.ActionType == actionType);

}
