// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Events;

using Runtime;

/// <summary>
/// Published when the in-process runtime has produced a new immutable state snapshot.
/// </summary>
public sealed record RuntimeSnapshotChangedEvent(MobaRuntimeSnapshot Snapshot) : EventBase;

/// <summary>
/// Published when MOBApi forwards a MOBAflow runtime snapshot to MOBAsmart.
/// </summary>
public sealed record RemoteRuntimeSnapshotChangedEvent(MobaRuntimeSnapshot Snapshot) : EventBase;