// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Z21;

/// <summary>
/// Documents Z21 LAN broadcast prerequisites from protocol specification v1.13.
/// Used by tests and architecture docs; values mirror docs/z21-lan-protokoll.pdf.
/// </summary>
public static class Z21BroadcastRequirements
{
    /// <summary>Driving and switching broadcasts (track power, loco info, turnout info).</summary>
    public const uint DrivingBroadcastFlag = 0x0000_0001;

    /// <summary>R-Bus occupancy feedback.</summary>
    public const uint RbusBroadcastFlag = 0x0000_0002;

    /// <summary>System state (current, temperature, voltage).</summary>
    public const uint SystemStateBroadcastFlag = 0x0000_0100;

    /// <summary>All loco info without per-address subscription (high traffic, PC only).</summary>
    public const uint AllLocoInfoBroadcastFlag = 0x0001_0000;

    /// <summary>Max subscribed locomotive addresses per client without <see cref="AllLocoInfoBroadcastFlag"/>.</summary>
    public const int MaxSubscribedLocomotiveAddresses = 16;

    /// <summary>
    /// MOBAflow basic subscription: driving + R-Bus + system state (not AllLocoInfo).
    /// </summary>
    public const uint MobaFlowBasicBroadcastFlags = DrivingBroadcastFlag | RbusBroadcastFlag | SystemStateBroadcastFlag;
}
