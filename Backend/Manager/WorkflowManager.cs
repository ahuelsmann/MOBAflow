using Moba.Backend.Model;

using System.Diagnostics;

namespace Moba.Backend.Manager;

/// <summary>
/// Manages the execution of workflows and their actions based on feedback events (track feedback points) independent of a journey.
/// </summary>
public class WorkflowManager : BaseFeedbackManager<Workflow>
{
    private readonly SemaphoreSlim _processingLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the WorkflowManager class.
    /// </summary>
    /// <param name="z21">Z21 command station for receiving feedback events</param>
    /// <param name="workflows">List of workflows to manage</param>
    /// <param name="executionContext">Optional execution context; if null, a new context with Z21 will be created</param>
    public WorkflowManager(Z21 z21, List<Workflow> workflows, Model.Action.ActionExecutionContext? executionContext = null)
    : base(z21, workflows, executionContext)
    {
    }

    protected override async Task ProcessFeedbackAsync(FeedbackResult feedback)
    {
        // Wait for lock (blocking) - queues feedbacks sequentially
        await _processingLock.WaitAsync();

        try
        {
            Debug.WriteLine($"📡 Workflow feedback received: InPort {feedback.InPort}");

            foreach (var workflow in Entities)
            {
                if (GetInPort(workflow) == feedback.InPort)
                {
                    if (ShouldIgnoreFeedback(workflow))
                    {
                        Debug.WriteLine($"⏭ Feedback for workflow '{GetEntityName(workflow)}' ignored (timer active)");
                        continue;
                    }

                    UpdateLastFeedbackTime(GetInPort(workflow));
                    await HandleWorkflowFeedbackAsync(workflow);
                }
            }
        }
        finally
        {
            _processingLock.Release();
        }
    }

    private async Task HandleWorkflowFeedbackAsync(Workflow workflow)
    {
        try
        {
            Debug.WriteLine($"▶ Executing workflow '{workflow.Name}'");

            await workflow.StartAsync(ExecutionContext);

            Debug.WriteLine($"✅ Workflow '{workflow.Name}' completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error in workflow '{workflow.Name}': {ex.Message}");
        }
    }

    protected override uint GetInPort(Workflow entity) => entity.InPort;

    protected override bool IsUsingTimerToIgnoreFeedbacks(Workflow entity) => entity.IsUsingTimerToIgnoreFeedbacks;

    protected override double GetIntervalForTimerToIgnoreFeedbacks(Workflow entity) => entity.IntervalForTimerToIgnoreFeedbacks;

    protected override string GetEntityName(Workflow entity) => entity.Name;

    public override void ResetAll()
    {
        base.ResetAll();
        Debug.WriteLine("🔄 All workflow timers reset");
    }

    protected override void CleanupResources()
    {
        _processingLock.Dispose();
    }
}
