// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel.Action;

using Domain;
using Domain.Enum;

/// <summary>
/// ViewModel for train destination display workflow actions.
/// </summary>
public sealed class TrainDestinationDisplayViewModel : WorkflowActionViewModel
{
    public TrainDestinationDisplayViewModel(WorkflowAction action) : base(action, ActionType.TrainDestinationDisplay)
    {
        action.TrainDestinationDisplay ??= new TrainDestinationDisplayActionPayload();
    }

    private TrainDestinationDisplayActionPayload Payload => UnderlyingAction.TrainDestinationDisplay ??= new TrainDestinationDisplayActionPayload();

    /// <summary>
    /// Gets the currently available display devices.
    /// </summary>
    public IReadOnlyList<DisplayDeviceOption> AvailableDisplayDevices { get; } = [];

    /// <summary>
    /// Gets or sets the selected display device ID.
    /// </summary>
    public Guid DisplayDeviceId
    {
        get => Payload.DisplayDeviceId;
        set
        {
            if (!SetProperty(Payload.DisplayDeviceId, value, Payload, (p, v) => p.DisplayDeviceId = v))
                return;

            OnPropertyChanged(nameof(SelectedDeviceName));
        }
    }

    /// <summary>
    /// Gets a readable label for the selected display device.
    /// </summary>
    public string SelectedDeviceName =>
        AvailableDisplayDevices.FirstOrDefault(device => device.Id == DisplayDeviceId)?.Name
        ?? "No display device selected";

    /// <summary>
    /// Gets or sets whether the display is cleared before rendering.
    /// </summary>
    public bool ClearBeforeRender
    {
        get => Payload.ClearBeforeRender;
        set => SetProperty(Payload.ClearBeforeRender, value, Payload, (p, v) => p.ClearBeforeRender = v);
    }
}

/// <summary>
/// Lightweight display device option for action editor bindings.
/// </summary>
public sealed record DisplayDeviceOption(Guid Id, string Name);
