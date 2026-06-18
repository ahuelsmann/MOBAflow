// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55252 - Curved track R2, r = 16.61" (421.88 mm) / 7,5°, 48 pieces / circle.
/// </summary>
public sealed record R275() : Curved(7.5, 421.88);