namespace Moba.Backend.Model.Action;

using Enum;

using Interface;

public abstract class Base : IAction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int Number { get; set; }
    public virtual ActionType Type { get; set; }
    public IList<IAction> Actions { get; set; } = [];

    /// <summary>
    /// Executes this action asynchronously with the provided execution context
    /// </summary>
    /// <param name="context">Execution context containing dependencies (Z21, SpeakerEngine, etc.)</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public abstract Task ExecuteAsync(ActionExecutionContext context);

    /// <summary>
    /// Executes this action asynchronously without context (for backward compatibility)
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task ExecuteAsync()
    {
        return ExecuteAsync(new ActionExecutionContext());
    }
}