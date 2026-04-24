// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Microsoft.Extensions.Logging;

using Common.Events;
using TrackLibrary.PikoA;

using System.Diagnostics;

/// <summary>
/// Visualises Z21 R-Bus feedback events on the track plan canvas.
///
/// Flow:
/// 1. Subscribes to <see cref="FeedbackReceivedEvent"/> on the shared <see cref="IEventBus"/>.
/// 2. Resolves the 1-based Z21 InPort onto all <c>PlacedSegment</c>s via <see cref="EditableTrackPlan.GetSegmentIdsByInPort(int)"/>.
/// 3. Starts/keeps a per-segment pulse that <c>TrackPlanPage.GraphCanvasControl_Draw</c> queries
///    via <see cref="GetPulseIntensity(Guid)"/>.
/// 4. A lightweight background timer raises <see cref="HighlightsChanged"/> at ~30 fps while at
///    least one pulse is active; the timer stops automatically when all pulses expire.
///
/// Re-triggers for an InPort during an active pulse are absorbed (Journey-Timer-Semantics,
/// see <c>Journey.IsUsingTimerToIgnoreFeedbacks</c> / <c>IntervalForTimerToIgnoreFeedbacks</c>).
/// </summary>
public sealed class TrackPlanFeedbackHighlighter : IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly EditableTrackPlan _plan;
    private readonly ILogger<TrackPlanFeedbackHighlighter>? _logger;

    private readonly object _gate = new();
    private readonly Dictionary<Guid, long> _pulseStartTicks = [];
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private Timer? _frameTimer;
    private Guid _subscriptionId = Guid.Empty;
    private bool _disposed;

    /// <summary>Total duration of the pulse animation before it fades out completely.</summary>
    public TimeSpan HoldDuration { get; set; } = TimeSpan.FromMilliseconds(1500);

    /// <summary>
    /// Minimum time between two pulse activations for the same segment.
    /// Matches the Journey-Timer semantics
    /// (<c>Journey.IntervalForTimerToIgnoreFeedbacks</c>).
    /// </summary>
    public TimeSpan IgnoreWindow { get; set; } = TimeSpan.FromMilliseconds(1500);

    /// <summary>Fires when the set of active highlights or their intensities changed.</summary>
    public event EventHandler? HighlightsChanged;

    public TrackPlanFeedbackHighlighter(
        IEventBus eventBus,
        EditableTrackPlan plan,
        ILogger<TrackPlanFeedbackHighlighter>? logger = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _logger = logger;
    }

    /// <summary>Subscribes the highlighter to the event bus. Call once during application startup.</summary>
    public void Activate()
    {
        if (_subscriptionId != Guid.Empty)
            return;
        _subscriptionId = _eventBus.Subscribe<FeedbackReceivedEvent>(OnFeedbackReceived);
    }

    /// <summary>
    /// Returns the current pulse intensity (0..1) for a placed segment.
    /// Returns <c>0</c> if no pulse is active or the pulse has expired.
    /// </summary>
    public double GetPulseIntensity(Guid segmentId)
    {
        long nowTicks = _clock.ElapsedTicks;
        long startTicks;
        lock (_gate)
        {
            if (!_pulseStartTicks.TryGetValue(segmentId, out startTicks))
                return 0;
        }

        var elapsed = TimeSpan.FromTicks(nowTicks - startTicks);
        if (elapsed >= HoldDuration || elapsed < TimeSpan.Zero)
            return 0;

        // Blink component: |sin| at ~4 Hz.
        // Fade component: linear ramp from 1.0 -> 0.0 across HoldDuration.
        double t = elapsed.TotalSeconds;
        double blink = Math.Abs(Math.Sin(t * Math.PI * 4.0));
        double fade = 1.0 - (elapsed.TotalSeconds / HoldDuration.TotalSeconds);
        double intensity = blink * fade;
        return intensity < 0 ? 0 : intensity > 1 ? 1 : intensity;
    }

    /// <summary>True if at least one segment currently has an active pulse.</summary>
    public bool HasActiveHighlights
    {
        get
        {
            lock (_gate)
            {
                return _pulseStartTicks.Count > 0;
            }
        }
    }

    /// <summary>
    /// Evicts all pulses whose <see cref="HoldDuration"/> has elapsed.
    /// Called from the frame timer; exposed publicly so tests can force purging deterministically.
    /// </summary>
    public void PurgeExpiredPulses()
    {
        lock (_gate)
        {
            PurgeExpired();
        }
    }

    private void OnFeedbackReceived(FeedbackReceivedEvent e)
    {
        try
        {
            long nowTicks = _clock.ElapsedTicks;
            long ignoreTicks = IgnoreWindow.Ticks;
            bool changed = false;

            lock (_gate)
            {
                foreach (var segmentId in _plan.GetSegmentIdsByInPort(e.InPort))
                {
                    if (_pulseStartTicks.TryGetValue(segmentId, out long startTicks)
                        && nowTicks - startTicks < ignoreTicks)
                    {
                        // Still inside the ignore-window of the previous pulse; absorb.
                        continue;
                    }

                    _pulseStartTicks[segmentId] = nowTicks;
                    changed = true;
                }
            }

            if (changed)
            {
                EnsureFrameTimer();
                RaiseHighlightsChanged();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Feedback highlight processing failed for InPort={InPort}", e.InPort);
        }
    }

    private void EnsureFrameTimer()
    {
        if (_frameTimer != null)
            return;

        lock (_gate)
        {
            _frameTimer ??= new Timer(OnFrameTick, null, TimeSpan.FromMilliseconds(33), TimeSpan.FromMilliseconds(33));
        }
    }

    private void OnFrameTick(object? state)
    {
        _ = state;
        if (_disposed)
            return;

        bool anyActive;
        lock (_gate)
        {
            PurgeExpired();
            anyActive = _pulseStartTicks.Count > 0;
        }

        RaiseHighlightsChanged();

        if (!anyActive)
        {
            lock (_gate)
            {
                _frameTimer?.Dispose();
                _frameTimer = null;
            }
        }
    }

    private void PurgeExpired()
    {
        long nowTicks = _clock.ElapsedTicks;
        long holdTicks = HoldDuration.Ticks;

        List<Guid>? expired = null;
        foreach (var kv in _pulseStartTicks)
        {
            if (nowTicks - kv.Value >= holdTicks)
            {
                expired ??= [];
                expired.Add(kv.Key);
            }
        }
        if (expired == null)
            return;
        foreach (var id in expired)
            _pulseStartTicks.Remove(id);
    }

    private void RaiseHighlightsChanged()
    {
        try
        {
            HighlightsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "HighlightsChanged handler threw");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_subscriptionId != Guid.Empty)
        {
            _eventBus.Unsubscribe(_subscriptionId);
            _subscriptionId = Guid.Empty;
        }

        lock (_gate)
        {
            _frameTimer?.Dispose();
            _frameTimer = null;
            _pulseStartTicks.Clear();
        }
    }
}
