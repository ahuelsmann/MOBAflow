// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using Domain;
using Domain.Enum;

/// <summary>
/// ViewModel for Z21 Command actions (loco control).
/// Wraps WorkflowAction with typed properties for Address, Speed, Direction.
/// </summary>
public class CommandViewModel : WorkflowActionViewModel
{
    #region Fields
    // (No additional fields - inherits from WorkflowActionViewModel)
    #endregion

    public CommandViewModel(WorkflowAction action) : base(action, ActionType.Command) { }

    /// <summary>
    /// Locomotive address (DCC address).
    /// </summary>
    public int Address
    {
        get => GetParameter<int>("Address");
        set => SetParameter("Address", value);
    }

    /// <summary>
    /// Speed (0-127 for DCC).
    /// </summary>
    public int Speed
    {
        get => GetParameter<int>("Speed");
        set => SetParameter("Speed", value);
    }

    /// <summary>
    /// Direction: "Forward" or "Backward".
    /// </summary>
    public string Direction
    {
        get => GetParameter<string>("Direction") ?? "Forward";
        set => SetParameter("Direction", value);
    }

    /// <summary>
    /// Raw command bytes (optional, for advanced users).
    /// </summary>
    public byte[]? Bytes
    {
        get => GetParameter<byte[]>("Bytes");
        set => SetParameter("Bytes", value);
    }

    public override string ToString() => !string.IsNullOrEmpty(Name) ? $"{Name} (Command)" : $"Command - Addr:{Address} Speed:{Speed} Dir:{Direction}";
}
