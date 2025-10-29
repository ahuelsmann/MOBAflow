namespace Moba.Backend.Model.Action;

using Enum;

using Interface;

/// <summary>
/// Abstract base class for all actions as part of a workflow.
/// Provides common properties (Id, Name, Number) and implements the IAction interface.
/// Derived classes must implement ExecuteAsync to define the specific action behavior.
/// </summary>
public abstract class Base : IAction
{
    /// <summary>
    /// Unique ID for an action.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Name of an action. The user can use this property as free text to provide a short description of the action.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Can be used to set the order in which actions are processed.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Specifies the type of this action (e.g., Command, Announcement, Sound).
    /// Used for type identification and UI rendering. Derived classes override this to return their specific type.
    /// </summary>
    public virtual ActionType Type { get; set; }

    /// <summary>
    /// List of additional actions that will be performed after this action.
    /// </summary>
    public IList<IAction> Actions { get; set; } = [];

    /// <summary>
    /// Executes this action asynchronously with the provided execution context.
    /// Derived classes must implement this method to define their specific behavior.
    /// </summary>
    /// <param name="context">Execution context containing dependencies (Z21, SpeakerEngine, Project settings, etc.)</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public abstract Task ExecuteAsync(ActionExecutionContext context);
}