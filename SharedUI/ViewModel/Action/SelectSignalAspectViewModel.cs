// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using Domain;
using Domain.Enum;

/// <summary>
/// ViewModel for workflow actions that set a signal aspect through DCC accessory commands.
/// </summary>
public sealed class SelectSignalAspectViewModel : WorkflowActionViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectSignalAspectViewModel"/> class.
    /// </summary>
    /// <param name="action">The underlying workflow action.</param>
    public SelectSignalAspectViewModel(WorkflowAction action) : base(action, ActionType.SelectSignalAspect)
    {
        action.SelectSignalAspect ??= new SelectSignalAspectActionPayload();
    }

    private SelectSignalAspectActionPayload Payload => UnderlyingAction.SelectSignalAspect ??= new SelectSignalAspectActionPayload();

    /// <summary>
    /// Gets all available signal aspect values for ComboBox binding.
    /// </summary>
    public SignalAspect[] SignalAspectValues { get; } = Enum.GetValues<SignalAspect>();

    /// <summary>
    /// Gets or sets the base DCC accessory address.
    /// </summary>
    public int BaseAddress
    {
        get => Payload.BaseAddress;
        set => SetProperty(Payload.BaseAddress, value, Payload, (p, v) => p.BaseAddress = v);
    }

    /// <summary>
    /// Gets or sets the desired signal aspect.
    /// </summary>
    public SignalAspect SignalAspect
    {
        get => Payload.SignalAspect;
        set => SetProperty(Payload.SignalAspect, value, Payload, (p, v) => p.SignalAspect = v);
    }

    /// <summary>
    /// Gets or sets the Viessmann multiplexer article number.
    /// </summary>
    public string MultiplexerArticleNumber
    {
        get => Payload.MultiplexerArticleNumber;
        set => SetProperty(Payload.MultiplexerArticleNumber, value, Payload, (p, v) => p.MultiplexerArticleNumber = v);
    }

    /// <summary>
    /// Gets or sets the signal article number used for aspect mapping.
    /// </summary>
    public string SignalArticleNumber
    {
        get => Payload.SignalArticleNumber;
        set => SetProperty(Payload.SignalArticleNumber, value, Payload, (p, v) => p.SignalArticleNumber = v);
    }
}