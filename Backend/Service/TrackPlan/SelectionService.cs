// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service.TrackPlan;

/// <summary>Application-owned selection state, independent of WinUI controls.</summary>
public sealed class SelectionService
{
    public Guid? SelectedTrackId { get; private set; }

    public event EventHandler? SelectionChanged;

    public void Select(Guid? trackId)
    {
        if (SelectedTrackId == trackId)
            return;
        SelectedTrackId = trackId;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
