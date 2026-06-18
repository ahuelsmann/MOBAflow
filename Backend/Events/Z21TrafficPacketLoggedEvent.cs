// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Events;

using Common.Events;

using Model;

/// <summary>
/// Published when the Z21 traffic monitor logs a packet.
/// </summary>
public sealed record Z21TrafficPacketLoggedEvent(Z21TrafficPacket Packet) : EventBase;