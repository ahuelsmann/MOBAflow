namespace Moba.Display.Rendering;

using Moba.Domain;

public sealed record TrainDestinationDisplayRenderContext(
    Journey Journey,
    Station Station,
    Train? Train,
    DateTime Timestamp);