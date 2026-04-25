// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Manager;

using Domain;

public sealed class PlatformChangedEventArgs : EventArgs
{
    public required Station Station { get; init; }

    public required Platform Platform { get; init; }

    public required PlatformSessionState SessionState { get; init; }
}
