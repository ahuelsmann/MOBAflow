// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackLibrary.Base.TrackSystem;

/// <summary>
/// Represents a track end (connection point) on a track piece.
/// Each track piece has one or more ends where it can connect to other tracks.
/// </summary>
public sealed record TrackEnd(
    string Id,
    double AngleDeg
);
