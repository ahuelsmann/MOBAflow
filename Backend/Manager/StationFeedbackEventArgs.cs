// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Manager;

using Domain;

public sealed class StationFeedbackEventArgs : EventArgs
{
    public required Station Station { get; init; }

    public required StationSessionState SessionState { get; init; }
}