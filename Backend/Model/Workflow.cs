using System.Diagnostics;

namespace Moba.Backend.Model;

public class Workflow
{
    public Workflow()
    {
        Id = Guid.NewGuid();
        Name = "New Flow";
        Actions = [];
    }

    /// <summary>
    /// Unique identifier for this workflow
    /// </summary>
    public Guid Id { get; set; }

    public string Name { get; set; }

    public List<Action.Base> Actions { get; set; }

    /// <summary>
    /// R-BUS port assignment for this workflow
    /// </summary>
    public uint InPort { get; set; }

    /// <summary>
    /// Ignore repeated feedbacks
    /// </summary>
    public bool IsUsingTimerToIgnoreFeedbacks { get; set; }

    /// <summary>
    /// Ignore repeated feedbacks for x seconds
    /// </summary>
    public double IntervalForTimerToIgnoreFeedbacks { get; set; }

    /// <summary>
    /// Starts the execution of all actions in this workflow
    /// </summary>
    /// <param name="context">Execution context containing dependencies (Z21, SpeakerEngine, etc.)</param>
    /// <exception cref="InvalidOperationException">Thrown when the workflow has no actions</exception>
    public async Task StartAsync(Action.ActionExecutionContext? context = null)
    {
        if (Actions.Count == 0)
        {
            throw new InvalidOperationException($"Every workflow must have at least one action. Workflow: '{Name}' (ID: {Id})");
        }

        Debug.WriteLine($"▶ Starting workflow '{Name}' (ID: {Id}) with {Actions.Count} action(s)");

        // Create default context if none provided
        context ??= new Action.ActionExecutionContext();

        try
        {
            foreach (var action in Actions)
            {
                Debug.WriteLine($"  🔧 Executing action: {action.Name} ({action.Type})");

                await action.ExecuteAsync(context);
            }

            Debug.WriteLine($"✅ Workflow '{Name}' completed successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error in workflow '{Name}': {ex.Message}");
            throw;
        }
    }
}