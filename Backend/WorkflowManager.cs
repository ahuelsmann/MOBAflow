using Moba.Backend.Model;

using System.Diagnostics;

namespace Moba.Backend;

/// <summary>
/// Manages the execution of workflows and there actions based on feedback events (track feedback points) independent of a journey.
/// </summary>
public class WorkflowManager : IDisposable
{
    private readonly Z21 _z21;
    private readonly List<Workflow> _workflows;
    private readonly Dictionary<uint, DateTime> _lastFeedbackTime = new();
    private bool _isProcessing;
    private bool _disposed;
    private readonly Model.Action.ActionExecutionContext _executionContext;

    public WorkflowManager(Z21 z21, List<Workflow> workflows, Model.Action.ActionExecutionContext? executionContext = null)
    {
        _z21 = z21;
        _workflows = workflows;
        _z21.Received += OnFeedbackReceived;

        // Create execution context with Z21
        _executionContext = executionContext ?? new Model.Action.ActionExecutionContext
        {
            Z21 = z21
        };
    }

    private async void OnFeedbackReceived(FeedbackResult feedback)
    {
        if (_isProcessing)
        {
            Debug.WriteLine("⏸ Workflow feedback ignored - Processing already in progress");
            return;
        }

        Debug.WriteLine($"📡 Workflow feedback received: InPort {feedback.InPort}");

        foreach (var workflow in _workflows)
        {
            if (workflow.InPort == feedback.InPort)
            {
                if (ShouldIgnoreFeedback(workflow))
                {
                    Debug.WriteLine($"⏭ Feedback for workflow '{workflow.Name}' ignored (timer active)");
                    continue;
                }

                UpdateLastFeedbackTime(workflow.InPort);
                await HandleWorkflowFeedbackAsync(workflow);
            }
        }
    }

    private bool ShouldIgnoreFeedback(Workflow workflow)
    {
        if (!workflow.IsUsingTimerToIgnoreFeedbacks)
        {
            return false;
        }

        if (_lastFeedbackTime.TryGetValue(workflow.InPort, out DateTime lastTime))
        {
            var elapsed = (DateTime.UtcNow - lastTime.ToUniversalTime()).TotalSeconds;
            return elapsed < workflow.IntervalForTimerToIgnoreFeedbacks;
        }

        return false;
    }

    private void UpdateLastFeedbackTime(uint inPort)
    {
        _lastFeedbackTime[inPort] = DateTime.UtcNow;
    }

    private async Task HandleWorkflowFeedbackAsync(Workflow workflow)
    {
        _isProcessing = true;

        try
        {
            Debug.WriteLine($"▶ Executing workflow '{workflow.Name}'");

            await workflow.StartAsync(_executionContext);

            Debug.WriteLine($"✅ Workflow '{workflow.Name}' completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error in workflow '{workflow.Name}': {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    public void ResetAll()
    {
        _lastFeedbackTime.Clear();
        Debug.WriteLine("🔄 All workflow timers reset");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _z21.Received -= OnFeedbackReceived;
        }

        _disposed = true;
    }
}