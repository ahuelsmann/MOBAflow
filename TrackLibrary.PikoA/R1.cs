// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55211 - Curved track R1, r = 14.17" (360 mm) / 30°, 12 pieces / circle.
/// </summary>
public sealed record R1() : Curved(30, 360);