// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Service;

using Domain;

using System.Diagnostics;

/// <summary>
/// Manages track segment occupation state based on Z21 InPort feedback.
/// Rückmelder sind Zustandswechsel im Track-Graph, nicht UI-Events.
/// </summary>
public class FeedbackStateManager
{
    private readonly Dictionary<uint, TrackSegment> _inPortToSegmentMap = new();

    public event EventHandler<FeedbackStateChangedEventArgs>? FeedbackStateChanged;

    public void RegisterSegments(IEnumerable<TrackSegment> segments)
    {
        _inPortToSegmentMap.Clear();

        foreach (var segment in segments)
        {
            if (segment.AssignedInPort.HasValue)
            {
                _inPortToSegmentMap[segment.AssignedInPort.Value] = segment;
                Debug.WriteLine($"📍 Registered feedback: InPort {segment.AssignedInPort.Value} → Segment {segment.Id} ({segment.ArticleCode})");
            }
        }

        Debug.WriteLine($"📍 FeedbackStateManager: Registered {_inPortToSegmentMap.Count} feedback points");
    }

    public void UpdateFeedbackState(uint inPort, bool isOccupied)
    {
        if (!_inPortToSegmentMap.TryGetValue(inPort, out var segment))
        {
            Debug.WriteLine($"⚠️ FeedbackStateManager: InPort {inPort} not registered (no segment assigned)");
            return;
        }

        if (segment.IsOccupied == isOccupied)
            return;

        segment.IsOccupied = isOccupied;

        var stateText = isOccupied ? "OCCUPIED" : "FREE";
        Debug.WriteLine($"🚦 FeedbackStateManager: InPort {inPort} → Segment {segment.Id} ({segment.ArticleCode}) is now {stateText}");

        FeedbackStateChanged?.Invoke(this, new FeedbackStateChangedEventArgs
        {
            InPort = inPort,
            SegmentId = segment.Id,
            ArticleCode = segment.ArticleCode,
            IsOccupied = isOccupied
        });
    }

    public bool IsSegmentOccupied(string segmentId, TrackLayout layout)
    {
        var segment = layout.Segments.FirstOrDefault(s => s.Id == segmentId);
        return segment?.IsOccupied ?? false;
    }

    public IEnumerable<TrackSegment> GetOccupiedSegments()
    {
        return _inPortToSegmentMap.Values.Where(s => s.IsOccupied);
    }

    public void ClearAllStates()
    {
        foreach (var segment in _inPortToSegmentMap.Values)
        {
            if (segment.IsOccupied)
            {
                segment.IsOccupied = false;
                FeedbackStateChanged?.Invoke(this, new FeedbackStateChangedEventArgs
                {
                    InPort = segment.AssignedInPort!.Value,
                    SegmentId = segment.Id,
                    ArticleCode = segment.ArticleCode,
                    IsOccupied = false
                });
            }
        }

        Debug.WriteLine("🚦 FeedbackStateManager: Cleared all occupation states");
    }
}

public class FeedbackStateChangedEventArgs : EventArgs
{
    public uint InPort { get; set; }
    public string SegmentId { get; set; } = string.Empty;
    public string ArticleCode { get; set; } = string.Empty;
    public bool IsOccupied { get; set; }
}
