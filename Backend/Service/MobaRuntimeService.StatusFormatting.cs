// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;
/// <summary>
/// Status text and signal-polarity helpers for <see cref="MobaRuntimeService"/>.
/// </summary>
public sealed partial class MobaRuntimeService
{
    private bool ShouldInvertPolarityForOffset(int addressOffset)
    {
        return _settings.SignalBox.GetInvertPolarityForOffset(addressOffset);
    }

    private string BuildSystemStateStatusText(SystemState systemState)
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

    private string GetConnectedStatusText()
    {
        _hasSeenSuccessfulZ21Connection = true;
        _isManualDisconnectRequested = false;
        return $"Connected to {_settings.Z21.CurrentIpAddress}";
    }

    private string GetDisconnectedStatusText()
    {
        return _isManualDisconnectRequested
            ? "Disconnected"
            : "Z21 disconnected";
    }
}
