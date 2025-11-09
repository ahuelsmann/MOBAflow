using Moba.Backend.Monitor;
using System.Diagnostics;

namespace Moba.Backend.Manager;

/// <summary>
/// Manages feedback monitoring and statistics collection for external clients (e.g., mobile apps, dashboards).
/// Unlike WorkflowManager, JourneyManager, and PlatformManager which process feedback for specific entities,
/// this manager collects raw feedback statistics from all InPorts without filtering or entity-specific logic.
/// </summary>
public class FeedbackMonitorManager : IDisposable
{
    private readonly Z21 _z21;
    private readonly FeedbackMonitor _feedbackMonitor;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the FeedbackMonitorManager class.
    /// </summary>
    /// <param name="z21">Z21 command station for receiving feedback events</param>
    /// <param name="feedbackMonitor">The feedback monitor for tracking statistics</param>
    public FeedbackMonitorManager(Z21 z21, FeedbackMonitor feedbackMonitor)
    {
        _z21 = z21;
        _feedbackMonitor = feedbackMonitor;
        _z21.Received += OnFeedbackReceived;

        Debug.WriteLine("📊 FeedbackMonitorManager initialized");
    }

    /// <summary>
    /// Handles incoming feedback events from Z21 and records them in the monitor.
    /// </summary>
    private void OnFeedbackReceived(FeedbackResult feedback)
    {
        // Record raw feedback without any filtering or entity logic
        _feedbackMonitor.RecordFeedback((uint)feedback.InPort);
        
        Debug.WriteLine($"📊 FeedbackMonitorManager: Recorded feedback for InPort {feedback.InPort}");
    }

    /// <summary>
    /// Resets all statistics in the feedback monitor.
    /// </summary>
    public void ResetAll()
    {
        _feedbackMonitor.ResetAll();
        Debug.WriteLine("🔄 FeedbackMonitorManager: All statistics reset");
    }

    /// <summary>
    /// Disposes the manager and unsubscribes from Z21 feedback events.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _z21.Received -= OnFeedbackReceived;
        _disposed = true;

        Debug.WriteLine("🗑 FeedbackMonitorManager disposed");
    }
}
