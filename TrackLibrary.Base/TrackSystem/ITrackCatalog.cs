// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackLibrary.Base.TrackSystem;

/// <summary>
/// Interface for accessing the catalog of available track templates.
/// This interface allows track systems (Piko A, Märklin, etc.) to provide their track templates
/// in a unified way for use in the track plan editor.
/// </summary>
public interface ITrackCatalog
{
    /// <summary>
    /// Unique identifier for this track system (e.g., "PikoA", "Maerklin", "Fleischmann").
    /// </summary>
    string SystemId { get; }

    /// <summary>
    /// Display name of the track system (e.g., "Piko A-Gleis").
    /// </summary>
    string SystemName { get; }

    /// <summary>
    /// Manufacturer of the track system (e.g., "Piko", "Märklin").
    /// </summary>
    string Manufacturer { get; }

    /// <summary>
    /// Scale of the track system (e.g., "H0", "N", "TT").
    /// </summary>
    string Scale { get; }

    /// <summary>
    /// All available track templates.
    /// </summary>
    IReadOnlyList<TrackTemplate> Templates { get; }

    /// <summary>
    /// All straight track templates.
    /// </summary>
    IEnumerable<TrackTemplate> Straights { get; }

    /// <summary>
    /// All curved track templates.
    /// </summary>
    IEnumerable<TrackTemplate> Curves { get; }

    /// <summary>
    /// All switch track templates.
    /// </summary>
    IEnumerable<TrackTemplate> Switches { get; }

    /// <summary>
    /// Get a template by its ID.
    /// </summary>
    TrackTemplate? GetById(string id);

    /// <summary>
    /// Get templates by geometry category.
    /// </summary>
    IEnumerable<TrackTemplate> GetByCategory(TrackGeometryKind kind);
}
