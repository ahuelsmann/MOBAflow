// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Events;

using Runtime;

/// <summary>
/// Published when the in-process runtime has produced a new immutable state snapshot.
/// </summary>
public sealed record RuntimeSnapshotChangedEvent(MobaRuntimeSnapshot Snapshot) : EventBase;
