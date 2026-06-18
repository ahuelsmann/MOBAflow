// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55251 - Curved track R1, r = 14.17" (360 mm) / 7,5°, 48 pieces / circle.
/// </summary>
public sealed record R175() : Curved(7.5, 360);