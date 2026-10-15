// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA;

/// <summary>
/// Connection between two port pairs in the editable track plan.
/// </summary>
public record PortConnection(Guid SourceSegment, string SourcePort, Guid TargetSegment, string TargetPort);
