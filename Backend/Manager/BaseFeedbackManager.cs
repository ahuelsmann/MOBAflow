// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Manager;

using CommunityToolkit.Mvvm.Messaging;
using Domain.Message;
using Interface;
using Service;
using System.Diagnostics;

/// <summary>
/// Abstract base class for feedback managers that handle Z21 track feedback events.
/// Provides common functionality for feedback processing, timer-based filtering, and resource management.
/// Derived classes must implement entity-specific feedback handling logic.
/// </summary>
/// <typeparam name="TEntity">The type of entity being managed (Workflow, Journey, Platform, etc.)</typeparam>
public abstract class BaseFeedbackManager<TEntity> : IFeedbackManager where TEntity : class
{
    protected readonly IZ21 Z21;
    protected readonly List<TEntity> Entities;
    protected readonly Dictionary<uint, DateTime> LastFeedbackTime = new();
    protected readonly ActionExecutionContext? ExecutionContext;
    protected bool Disposed;

    /// <summary>
    /// Initializes the base feedback manager with Z21 connection and entity list.
    /// </summary>
    /// <param name="z21">Z21 command station for receiving feedback events</param>
    /// <param name="entities">List of entities to manage</param>
    /// <param name="executionContext">Optional execution context; if null, a new context with Z21 will be created</param>
    protected BaseFeedbackManager(IZ21 z21, List<TEntity> entities, ActionExecutionContext? executionContext = null)
    {
        Z21 = z21;
        Entities = entities;
        
        // ✅ Subscribe to Messenger (new approach)
        // Handler is simple lambda that doesn't capture 'this', avoiding memory leak with WeakReferences
        WeakReferenceMessenger.Default.Register<FeedbackReceivedMessage>(
            this,
            (recipient, message) =>
            {
                OnMessageReceived(message);
            }
        );
        
        // Keep legacy Z21.Received subscription for backward compatibility
        Z21.Received += OnFeedbackReceived;

        ExecutionContext = executionContext ?? new ActionExecutionContext
        {
            Z21 = z21
        };
    }

    /// <summary>
    /// Handles incoming feedback events from Z21.
    /// Uses fire-and-forget pattern with proper exception handling to avoid blocking the event publisher.
    /// Exceptions are logged to Debug output.
    /// </summary>
    private void OnFeedbackReceived(FeedbackResult feedback)
    {
        // Fire-and-forget with exception handling
        // Cannot use async void here as it would swallow exceptions
        _ = Task.Run(async () =>
        {
            try
            {
                await ProcessFeedbackAsync(feedback).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ ProcessFeedbackAsync failed for InPort {feedback.InPort}: {ex.Message}");
                Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        });
    }

    /// <summary>
    /// Handles incoming feedback message from Messenger.
    /// Converts FeedbackReceivedMessage to FeedbackResult and processes it.
    /// Uses fire-and-forget pattern with proper exception handling.
    /// </summary>
    private void OnMessageReceived(FeedbackReceivedMessage message)
    {
        // Fire-and-forget with exception handling
        _ = Task.Run(async () =>
        {
            try
            {
                // Convert InPort (uint from ValueChangedMessage) to FeedbackResult
                var feedback = new FeedbackResult(message.RawData);
                await ProcessFeedbackAsync(feedback).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ ProcessFeedbackAsync failed for Messenger message: {ex.Message}");
                Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        });
    }

    /// <summary>
    /// Processes a feedback event. Derived classes must implement this to define
    /// how feedback is matched to entities and what actions should be taken.
    /// </summary>
    /// <param name="feedback">The feedback event from Z21</param>
    protected abstract Task ProcessFeedbackAsync(FeedbackResult feedback);

    /// <summary>
    /// Extracts the InPort number from an entity. Derived classes must implement this
    /// to specify how to get the InPort from their specific entity type.
    /// </summary>
    /// <param name="entity">The entity to extract InPort from</param>
    /// <returns>The InPort number associated with this entity</returns>
    protected abstract uint GetInPort(TEntity entity);

    /// <summary>
    /// Checks if an entity is using timer-based feedback filtering.
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <returns>True if timer filtering is enabled, false otherwise</returns>
    protected abstract bool IsUsingTimerToIgnoreFeedbacks(TEntity entity);

    /// <summary>
    /// Gets the timer interval (in seconds) for feedback filtering.
    /// </summary>
    /// <param name="entity">The entity to get the interval from</param>
    /// <returns>The interval in seconds</returns>
    protected abstract double GetIntervalForTimerToIgnoreFeedbacks(TEntity entity);

    /// <summary>
    /// Gets the display name of an entity for logging purposes.
    /// </summary>
    /// <param name="entity">The entity to get the name from</param>
    /// <returns>The entity's display name</returns>
    protected abstract string GetEntityName(TEntity entity);

    /// <summary>
    /// Determines whether feedback should be ignored based on timer settings.
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <returns>True if feedback should be ignored, false otherwise</returns>
    protected bool ShouldIgnoreFeedback(TEntity entity)
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
    /// <param name="inPort">The InPort number to update</param>
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
        Debug.WriteLine($"🔄 All {typeof(TEntity).Name} timers reset");
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
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
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
    /// Cleanup method for derived classes to release their specific resources (e.g., SemaphoreSlim).
    /// </summary>
    protected virtual void CleanupResources()
    {
        // Override in derived classes if needed
    }
}
