// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55214 - Curved track R4, r = 21.48" (545.63 mm) / 30°, 12 pieces / circle.
/// </summary>
public sealed record R4() : Curved(30, 545.63);
