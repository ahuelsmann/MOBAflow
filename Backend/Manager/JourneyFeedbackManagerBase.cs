// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Manager;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;

using Moba.Common.Extension;

using Service;

/// <summary>
/// Base class for Z21 track feedback handling for journeys (timer filtering, subscription lifecycle).
/// Only <see cref="JourneyManager"/> derives from this type; a generic abstraction was not justified (YAGNI).
/// </summary>
public abstract class JourneyFeedbackManagerBase : IDisposable
{
    protected readonly ILogger? Logger;

    /// <summary>
    /// Gets the Z21 instance used to receive feedback events and send commands.
    /// </summary>
    protected readonly IZ21 Z21;

    /// <summary>
    /// Gets the journeys managed for feedback matching and timer filtering.
    /// </summary>
    protected readonly List<Journey> Journeys;

    /// <summary>
    /// Tracks the last feedback time per InPort to support timer-based filtering.
    /// </summary>
    protected readonly Dictionary<uint, DateTime> LastFeedbackTime = [];

    /// <summary>
    /// Optional action execution context used when running workflows in response to feedback.
    /// </summary>
    protected readonly ActionExecutionContext? ExecutionContext;

    /// <summary>
    /// Indicates whether this manager has already been disposed.
    /// </summary>
    protected bool Disposed;

    /// <summary>
    /// Initializes the base journey feedback manager with Z21 connection and journey list.
    /// </summary>
    /// <param name="z21">Z21 command station for receiving feedback events</param>
    /// <param name="journeys">Journeys to evaluate for feedback handling</param>
    /// <param name="executionContext">Optional execution context; if null, a new context with Z21 will be created</param>
    /// <param name="logger">Optional logger</param>
    protected JourneyFeedbackManagerBase(
        IZ21 z21,
        List<Journey> journeys,
        ActionExecutionContext? executionContext = null,
        ILogger? logger = null)
    {
        Z21 = z21;
        Journeys = journeys;
        Logger = logger;

        Z21.Received += OnFeedbackReceived;

        ExecutionContext = executionContext ?? new ActionExecutionContext
        {
            Z21 = z21
        };
    }

    /// <summary>
    /// Handles incoming feedback events from Z21.
    /// Uses fire-and-forget pattern with proper exception handling to avoid blocking the event publisher.
    /// </summary>
    private void OnFeedbackReceived(FeedbackResult feedback)
    {
        try
        {
            ProcessFeedbackAsync(feedback).Observe(
                ex =>
                {
                    Logger?.LogWarning(ex, "ProcessFeedbackAsync failed for InPort {InPort}", feedback.InPort);
                });
        }
        catch (Exception ex)
        {
            Logger?.LogWarning(ex, "ProcessFeedbackAsync failed for InPort {InPort}", feedback.InPort);
        }
    }

    /// <summary>
    /// Processes a feedback event. Derived classes implement journey-specific logic.
    /// </summary>
    protected abstract Task ProcessFeedbackAsync(FeedbackResult feedback);

    /// <summary>
    /// Extracts the InPort number from a journey.
    /// </summary>
    protected abstract uint GetInPort(Journey entity);

    /// <summary>
    /// Checks if a journey uses timer-based feedback filtering.
    /// </summary>
    protected abstract bool IsUsingTimerToIgnoreFeedbacks(Journey entity);

    /// <summary>
    /// Gets the timer interval (in seconds) for feedback filtering.
    /// </summary>
    protected abstract double GetIntervalForTimerToIgnoreFeedbacks(Journey entity);

    /// <summary>
    /// Gets the display name of a journey for logging.
    /// </summary>
    protected abstract string GetEntityName(Journey entity);

    /// <summary>
    /// Determines whether feedback should be ignored based on timer settings.
    /// </summary>
    protected bool ShouldIgnoreFeedback(Journey entity)
    {
        if (!IsUsingTimerToIgnoreFeedbacks(entity))
        {
            return false;
        }

        uint inPort = GetInPort(entity);
        if (LastFeedbackTime.TryGetValue(inPort, out DateTime lastTime))
        {
            var elapsed = (DateTime.UtcNow - lastTime.ToUniversalTime()).TotalSeconds;
            return elapsed < GetIntervalForTimerToIgnoreFeedbacks(entity);
        }

        return false;
    }

    /// <summary>
    /// Updates the last feedback time for the specified InPort.
    /// </summary>
    protected void UpdateLastFeedbackTime(uint inPort)
    {
        LastFeedbackTime[inPort] = DateTime.UtcNow;
    }

    /// <summary>
    /// Resets all feedback timers and entity state.
    /// </summary>
    public virtual void ResetAll()
    {
        LastFeedbackTime.Clear();
        Logger?.LogDebug("All journey feedback timers reset");
    }

    /// <summary>
    /// Disposes the manager and unsubscribes from Z21 feedback events.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases resources used by the manager.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (Disposed)
        {
            return;
        }

        if (disposing)
        {
            Z21.Received -= OnFeedbackReceived;

            CleanupResources();
        }

        Disposed = true;
    }

    /// <summary>
    /// Cleanup hook for derived classes (e.g. SemaphoreSlim).
    /// </summary>
    protected virtual void CleanupResources()
    {
    }
}
