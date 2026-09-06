// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Events;

/// <summary>
/// Requests immediate MOBAsmart synchronization after a protected administrator credential was stored.
/// </summary>
public sealed record RemotePairingCompletedEvent(string IpAddress, int HttpPort) : EventBase;