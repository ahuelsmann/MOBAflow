// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.TrackSystem;

/// <summary>
/// Interface for track system catalogs (Piko A, Roco Line, Tillig Elite, etc.).
/// Provides access to track templates for a specific manufacturer's track system.
/// </summary>
public interface ITrackCatalog
{
    /// <summary>Unique identifier for the track system (e.g., "PikoA", "RocoLine")</summary>
    string SystemId { get; }

    /// <summary>Display name of the track system</summary>
    string SystemName { get; }

    /// <summary>Manufacturer name</summary>
    string Manufacturer { get; }

    /// <summary>Scale (e.g., "H0", "N", "TT")</summary>
    string Scale { get; }

    /// <summary>All available track templates</summary>
    IReadOnlyList<TrackTemplate> Templates { get; }

    /// <summary>Straight track templates</summary>
    IEnumerable<TrackTemplate> Straights { get; }

    /// <summary>Curve track templates</summary>
    IEnumerable<TrackTemplate> Curves { get; }

    /// <summary>Switch (Weiche) track templates</summary>
    IEnumerable<TrackTemplate> Switches { get; }

    /// <summary>Gets a template by its ID</summary>
    TrackTemplate? GetById(string id);

    /// <summary>Gets templates by geometry kind</summary>
    IEnumerable<TrackTemplate> GetByCategory(TrackGeometryKind kind);
}
