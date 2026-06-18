// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel.Action;

using Domain;
using Domain.Enum;

using Interface;

using Microsoft.Extensions.Logging;

using Sound;

/// <summary>
/// Creates workflow action models and their editor view models.
/// </summary>
public interface IWorkflowActionViewModelFactory
{
    /// <summary>
    /// Creates a default domain action for the requested action type.
    /// </summary>
    WorkflowAction CreateDefaultAction(ActionType actionType, uint number);

    /// <summary>
    /// Creates the editor view model for an existing domain action.
    /// </summary>
    WorkflowActionViewModel CreateViewModel(WorkflowAction action);

    /// <summary>
    /// Attempts to unwrap an action editor view model to its domain model.
    /// </summary>
    bool TryGetAction(object actionViewModel, out WorkflowAction action);
}

/// <summary>
/// Default action editor factory used by the workflow editor.
/// </summary>
public sealed class WorkflowActionViewModelFactory(
    IIoService ioService,
    ISoundPlayer? soundPlayer = null,
    ILogger<CommandViewModel>? commandLogger = null) : IWorkflowActionViewModelFactory
{
    private readonly IReadOnlyDictionary<ActionType, WorkflowActionViewModelDescriptor> _descriptors =
        WorkflowActionViewModelDescriptors.Create(ioService, soundPlayer, commandLogger);

    public WorkflowAction CreateDefaultAction(ActionType actionType, uint number)
    {
        if (!_descriptors.TryGetValue(actionType, out var descriptor))
            throw new ArgumentException($"Unsupported action type: {actionType}", nameof(actionType));

        return descriptor.CreateDefaultAction(number);
    }

    public WorkflowActionViewModel CreateViewModel(WorkflowAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!_descriptors.TryGetValue(action.Type, out var descriptor))
            throw new NotSupportedException($"Action type {action.Type} is not supported");

        return descriptor.CreateViewModel(action);
    }

    public bool TryGetAction(object actionViewModel, out WorkflowAction action)
    {
        if (actionViewModel is WorkflowActionViewModel workflowActionViewModel)
        {
            action = workflowActionViewModel.ToWorkflowAction();
            return true;
        }

        action = null!;
        return false;
    }
}

internal sealed class WorkflowActionViewModelDescriptor(
    ActionType actionType,
    Func<uint, WorkflowAction> createDefaultAction,
    Func<WorkflowAction, WorkflowActionViewModel> createViewModel)
{
    public ActionType ActionType { get; } = actionType;

    public WorkflowAction CreateDefaultAction(uint number) => createDefaultAction(number);

    public WorkflowActionViewModel CreateViewModel(WorkflowAction action) => createViewModel(action);
}

internal static class WorkflowActionViewModelDescriptors
{
    public static IReadOnlyDictionary<ActionType, WorkflowActionViewModelDescriptor> Create(
        IIoService ioService,
        ISoundPlayer? soundPlayer,
        ILogger<CommandViewModel>? commandLogger)
    {
        WorkflowActionViewModelDescriptor[] descriptors =
        [
            new(
                ActionType.Announcement,
                number => new WorkflowAction
                {
                    Name = "New Announcement",
                    Number = number,
                    Type = ActionType.Announcement,
                    Announcement = new AnnouncementActionPayload
                    {
                        Message = "New announcement text",
                        VoiceName = "de-DE-KatjaNeural"
                    }
                },
                action => new AnnouncementViewModel(action)),
            new(
                ActionType.Audio,
                number => new WorkflowAction
                {
                    Name = "New Audio",
                    Number = number,
                    Type = ActionType.Audio,
                    Audio = new AudioActionPayload
                    {
                        FilePath = "sound.wav"
                    }
                },
                action => new AudioViewModel(action, ioService, soundPlayer)),
            new(
                ActionType.Command,
                number => new WorkflowAction
                {
                    Name = "New Command",
                    Number = number,
                    Type = ActionType.Command,
                    Command = new CommandActionPayload
                    {
                        BytesBase64 = Convert.ToBase64String([0x00])
                    }
                },
                action => new CommandViewModel(action, commandLogger)),
            new(
                ActionType.ExecuteScript,
                number => new WorkflowAction
                {
                    Name = "New PowerShell Script",
                    Number = number,
                    Type = ActionType.ExecuteScript,
                    PowerShell = new PowerShellActionPayload()
                },
                action => new PowerShellActionViewModel(action)),
            new(
                ActionType.SelectSignalAspect,
                number => new WorkflowAction
                {
                    Name = "New Signal Aspect",
                    Number = number,
                    Type = ActionType.SelectSignalAspect,
                    SelectSignalAspect = new SelectSignalAspectActionPayload
                    {
                        BaseAddress = 1,
                        SignalAspect = SignalAspect.Hp0,
                        MultiplexerArticleNumber = "5229",
                        SignalArticleNumber = "4046"
                    }
                },
                action => new SelectSignalAspectViewModel(action)),
            new(
                ActionType.TrainDestinationDisplay,
                number => new WorkflowAction
                {
                    Name = "New Display Output",
                    Number = number,
                    Type = ActionType.TrainDestinationDisplay,
                    TrainDestinationDisplay = new TrainDestinationDisplayActionPayload
                    {
                        DisplayDeviceId = Guid.Empty,
                        ClearBeforeRender = true
                    }
                },
                action => new TrainDestinationDisplayViewModel(action))
        ];

        return descriptors.ToDictionary(descriptor => descriptor.ActionType);
    }
}