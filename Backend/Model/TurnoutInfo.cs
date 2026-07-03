// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Model;

/// <summary>
/// LAN_X_TURNOUT_INFO (0x43) response: low-level accessory decoder position only.
/// Does not carry KS signal aspects or multiplexer semantics.
/// </summary>
public sealed class TurnoutInfo
{
    /// <summary>Gets the Z21 function address (FAdr).</summary>
    public int FunctionAddress { get; init; }

    /// <summary>Gets whether the turnout has been switched at least once.</summary>
    public bool IsSwitched { get; init; }

    /// <summary>Gets the output position (P=0 or P=1) when <see cref="IsSwitched"/> is true.</summary>
    public bool OutputPosition { get; init; }
}
