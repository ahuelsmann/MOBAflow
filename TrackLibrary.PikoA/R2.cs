// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55212 - Curved track R2, r = 16.61" (421.88 mm) / 30°, 12 pieces / circle.
/// </summary>
public sealed record R2() : Curved(30, 421.88);
