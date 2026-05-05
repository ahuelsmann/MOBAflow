// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55213 - Curved track R3, r = 19.05" (483.75 mm) / 30°, 12 pieces / circle.
/// </summary>
public sealed record R3() : Curved(30, 483.75);
