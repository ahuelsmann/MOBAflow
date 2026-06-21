// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Runtime;

/// <summary>
/// Determines when MOBAsmart should keep the Android foreground service running.
/// </summary>
public static class MobileBackgroundKeepAlivePolicy
{
    /// <summary>
    /// Returns true when at least one active connection requires background keep-alive.
    /// </summary>
    public static bool ShouldKeepAlive(
        bool isLocalZ21Connected,
        bool isMobaflowConnectionEnabled,
        bool isRestApiReachable,
        bool isRuntimeHubConnected)
    {
        if (isLocalZ21Connected)
        {
            return true;
        }

        return isMobaflowConnectionEnabled && isRestApiReachable && isRuntimeHubConnected;
    }

    /// <summary>
    /// Builds the foreground-service notification message for the current connection mode.
    /// </summary>
    public static string GetNotificationMessage(bool isLocalZ21Connected, bool isMobaflowSessionActive)
    {
        if (isLocalZ21Connected && isMobaflowSessionActive)
        {
            return "Z21 and MOBAflow session active";
        }

        if (isMobaflowSessionActive)
        {
            return "MOBAflow session active";
        }

        return "Z21 connection maintained";
    }
}
