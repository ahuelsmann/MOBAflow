// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Interface;

public interface IAction
{
    /// <summary>
    /// Unique ID for an action.
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// Name of an action. The user can use this property as free text to provide a short description of the action.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Can be used to set the order in which actions are processed.
    /// </summary>
    int Number { get; set; }

    /// <summary>
    /// List of additional actions that will be performed after this action.
    /// </summary>
    IList<IAction> Actions { get; set; }
}
