// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Rendering;

using Moba.Domain;

public sealed record TrainDestinationDisplayRenderContext(
    Journey Journey,
    Station Station,
    Train? Train,
    DateTime Timestamp);