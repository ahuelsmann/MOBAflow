// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Backend;

/// <summary>
/// Formats Z21 connection and system-state status text for runtime snapshots.
/// </summary>
public static class MobaRuntimeStatusFormatter
{
    public static string BuildSystemStateStatusText(SystemState systemState)
    {
        List<string> warnings = [];
        if (systemState.IsEmergencyStop)
        {
            warnings.Add("EMERGENCY STOP");
        }

        if (systemState.IsShortCircuit)
        {
            warnings.Add("SHORT CIRCUIT");
        }

        if (systemState.IsProgrammingMode)
        {
            warnings.Add("Programming");
        }

        return warnings.Count > 0
            ? $"Connected | {string.Join(" | ", warnings)}"
            : "Connected";
    }

    public static string GetConnectedStatusText(string z21IpAddress)
    {
        return $"Connected to {z21IpAddress}";
    }

    public static string GetDisconnectedStatusText(bool isManualDisconnectRequested)
    {
        return isManualDisconnectRequested
            ? "Disconnected"
            : "Z21 disconnected";
    }
}
