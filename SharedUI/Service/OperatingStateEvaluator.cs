// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using ViewModel;

/// <summary>
/// Aggregates subsystem status into a single shell-wide operating state presentation.
/// </summary>
public static class OperatingStateEvaluator
{
    public static OperatingStatePresentation Evaluate(OperatingStateInput input)
    {
        OperatingStateKind nextState;
        string nextText;
        string nextDetail;
        string nextGlyph;
        bool nextShowInfoBar;
        bool nextFailSafe;

        if (!input.IsConnected)
        {
            if (input.IsManualDisconnectRequested)
            {
                nextState = OperatingStateKind.Recovering;
                nextText = "Offline";
                nextDetail = "Z21 manually disconnected.";
                nextGlyph = "\uE711";
                nextShowInfoBar = false;
                nextFailSafe = false;
            }
            else if (input.IsZ21Connecting || !input.HasSeenSuccessfulZ21Connection || input.IsPostStartupInitializationRunning)
            {
                nextState = OperatingStateKind.Recovering;
                nextText = "Recovering";
                nextDetail = string.IsNullOrWhiteSpace(input.StatusText)
                    ? "Connecting to the Z21..."
                    : input.StatusText;
                nextGlyph = "\uE895";
                nextShowInfoBar = false;
                nextFailSafe = false;
            }
            else
            {
                nextState = OperatingStateKind.FailSafe;
                nextText = "Fail-Safe Active";
                nextDetail = string.IsNullOrWhiteSpace(input.LastFailSafeReason)
                    ? "Unexpected loss of the Z21 connection."
                    : input.LastFailSafeReason;
                nextGlyph = "\uE7BA";
                nextShowInfoBar = true;
                nextFailSafe = true;
            }
        }
        else if (input.IsOperatorAckRequired)
        {
            nextState = OperatingStateKind.FailSafe;
            nextText = "Fail-Safe Active";
            nextDetail = "Connection recovered. Explicit operator release is required.";
            nextGlyph = "\uE7BA";
            nextShowInfoBar = true;
            nextFailSafe = true;
        }
        else
        {
            var issues = new List<string>();

            if (input.IsEmergencyStopActive)
            {
                issues.Add("Emergency stop active");
            }

            if (input.IsShortCircuitActive)
            {
                issues.Add("Short circuit detected");
            }

            if (input.IsProgrammingModeActive)
            {
                issues.Add("Programming mode active");
            }

            if (input.AutoStartWebApp && !input.RestApiIsReachable)
            {
                issues.Add("REST API not reachable");
            }

            if (input.SpeechHealthStatus.Contains("Failed", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add("Piper TTS unavailable");
            }

            if (issues.Count > 0)
            {
                nextState = OperatingStateKind.Degraded;
                nextText = "Degraded";
                nextDetail = string.Join(" | ", issues);
                nextGlyph = "\uE7BA";
                nextShowInfoBar = input.AutoStartWebApp && !input.RestApiIsReachable;
                nextFailSafe = false;
            }
            else
            {
                nextState = OperatingStateKind.Normal;
                nextText = "Normal";
                nextDetail = input.IsTrackPowerOn
                    ? "Z21 connected, track power on."
                    : "Z21 connected.";
                nextGlyph = "\uE73E";
                nextShowInfoBar = false;
                nextFailSafe = false;
            }
        }

        var nextInfoBarTitle = nextState switch
        {
            OperatingStateKind.FailSafe => "Fail-safe mode active",
            OperatingStateKind.Degraded => "Restricted operation",
            OperatingStateKind.Recovering => "Recovery in progress",
            _ => "Normal operation"
        };

        var nextInfoBarMessage = nextState switch
        {
            OperatingStateKind.FailSafe => $"{nextDetail} Critical control actions remain blocked until the system is released again.",
            OperatingStateKind.Degraded => $"{nextDetail} The shell remains usable, but one or more supporting services are degraded.",
            OperatingStateKind.Recovering => nextDetail,
            OperatingStateKind.Normal => nextDetail,
            _ => nextDetail,
        };

        var tooltipText = input.LastFailSafeAt.HasValue && nextFailSafe
            ? $"{nextInfoBarMessage} Last fail-safe trigger: {input.LastFailSafeAt.Value:HH:mm:ss}"
            : nextInfoBarMessage;

        return new OperatingStatePresentation
        {
            State = nextState,
            Text = nextText,
            DetailText = nextDetail,
            IconGlyph = nextGlyph,
            HasDetailText = !string.IsNullOrWhiteSpace(nextDetail),
            ShowInfoBar = nextShowInfoBar,
            InfoBarTitle = nextInfoBarTitle,
            InfoBarMessage = nextInfoBarMessage,
            IsFailSafeActive = nextFailSafe,
            IsOperationalControlEnabled = input.IsConnected && !nextFailSafe && nextState != OperatingStateKind.Recovering,
            TooltipText = tooltipText
        };
    }
}

/// <summary>
/// Inputs required to evaluate the shell-wide operating state.
/// </summary>
public sealed class OperatingStateInput
{
    public bool IsConnected { get; init; }
    public bool IsTrackPowerOn { get; init; }
    public string StatusText { get; init; } = string.Empty;
    public bool IsManualDisconnectRequested { get; init; }
    public bool IsZ21Connecting { get; init; }
    public bool HasSeenSuccessfulZ21Connection { get; init; }
    public bool IsPostStartupInitializationRunning { get; init; }
    public bool IsOperatorAckRequired { get; init; }
    public bool IsEmergencyStopActive { get; init; }
    public bool IsShortCircuitActive { get; init; }
    public bool IsProgrammingModeActive { get; init; }
    public string LastFailSafeReason { get; init; } = string.Empty;
    public DateTimeOffset? LastFailSafeAt { get; init; }
    public bool AutoStartWebApp { get; init; }
    public bool RestApiIsReachable { get; init; }
    public string SpeechHealthStatus { get; init; } = string.Empty;
}

/// <summary>
/// Operator-facing operating state presentation for the shell.
/// </summary>
public sealed class OperatingStatePresentation
{
    public OperatingStateKind State { get; init; }
    public string Text { get; init; } = string.Empty;
    public string DetailText { get; init; } = string.Empty;
    public string IconGlyph { get; init; } = string.Empty;
    public bool HasDetailText { get; init; }
    public bool ShowInfoBar { get; init; }
    public string InfoBarTitle { get; init; } = string.Empty;
    public string InfoBarMessage { get; init; } = string.Empty;
    public bool IsFailSafeActive { get; init; }
    public bool IsOperationalControlEnabled { get; init; }
    public string TooltipText { get; init; } = string.Empty;
}
