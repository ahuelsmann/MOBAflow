// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using System.Runtime.InteropServices;

/// <summary>
/// High-level operating states for the shell-wide status presentation.
/// This state is intentionally coarser than individual subsystem statuses.
/// </summary>
public enum OperatingStateKind
{
    Recovering,
    Normal,
    Degraded,
    FailSafe
}

/// <summary>
/// MainWindowViewModel - shell-wide operating state and fail-safe UI logic.
/// Aggregates subsystem status into a single operator-facing state for the status bar and InfoBar.
/// </summary>
public partial class MainWindowViewModel
{
    internal bool SuppressOperatingStateRecompute { get; set; }

    private bool _isZ21Connecting = true;
    private bool _hasSeenSuccessfulZ21Connection;
    private bool _isManualDisconnectRequested;
    private bool _isEmergencyStopActive;
    private bool _isShortCircuitActive;
    private bool _isProgrammingModeActive;
    private string _lastFailSafeReason = "Waiting for the Z21 connection.";
    private DateTimeOffset? _lastFailSafeAt;

    /// <summary>
    /// Current shell-wide operating state.
    /// </summary>
    [ObservableProperty]
    private OperatingStateKind _operatingState = OperatingStateKind.Recovering;

    /// <summary>
    /// Short operator-facing label shown in the status bar badge.
    /// </summary>
    [ObservableProperty]
    private string _operatingStateText = "Starting";

    /// <summary>
    /// Short detail text shown next to the status badge.
    /// </summary>
    [ObservableProperty]
    private string _operatingStateDetailText = "Initializing services...";

    /// <summary>
    /// Glyph used in the operating state badge.
    /// </summary>
    [ObservableProperty]
    private string _operatingStateIconGlyph = "\uE895";

    /// <summary>
    /// Tooltip text for the status badge.
    /// </summary>
    [ObservableProperty]
    private string _operatingStateTooltipText = "Initializing services...";

    /// <summary>
    /// Indicates whether a non-empty detail text is available for inline display.
    /// </summary>
    [ObservableProperty]
    private bool _hasOperatingStateDetailText = true;

    /// <summary>
    /// Shows the contextual InfoBar in the shell when immediate operator attention is required.
    /// </summary>
    [ObservableProperty]
    private bool _showOperatingStateInfoBar;

    /// <summary>
    /// Title for the shell InfoBar.
    /// </summary>
    [ObservableProperty]
    private string _operatingStateInfoBarTitle = "Starting";

    /// <summary>
    /// Message for the shell InfoBar.
    /// </summary>
    [ObservableProperty]
    private string _operatingStateInfoBarMessage = "Initializing services...";

    /// <summary>
    /// True when the shell is in a latched fail-safe condition.
    /// </summary>
    [ObservableProperty]
    private bool _isFailSafeActive;

    /// <summary>
    /// True when the operator must explicitly re-arm the system after an unexpected interruption.
    /// </summary>
    [ObservableProperty]
    private bool _isOperatorAckRequired;

    /// <summary>
    /// True when critical control operations may be executed.
    /// </summary>
    [ObservableProperty]
    private bool _isOperationalControlEnabled;

    /// <summary>
    /// Clears the latched fail-safe state after the connection has recovered.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAcknowledgeOperatingState))]
    private async Task AcknowledgeOperatingState()
    {
        if (!IsConnected)
            return;

        await _mobaRuntime.AcknowledgeFailSafeAsync().ConfigureAwait(false);
    }

    private bool CanAcknowledgeOperatingState() => IsOperatorAckRequired && IsConnected;

    private void SafeNotifyCanExecuteChanged(System.Action notifyCanExecuteChanged)
    {
        if (_isShuttingDown)
        {
            return;
        }

        try
        {
            notifyCanExecuteChanged();
        }
        catch (COMException) when (_isShuttingDown)
        {
        }
        catch (COMException ex)
        {
            _logger.LogDebug(ex, "Ignored WinRT command notification failure during UI teardown.");
        }
    }

    partial void OnStatusTextChanged(string value)
    {
        _ = value;
        if (SuppressOperatingStateRecompute) return;
        RecomputeOperatingState();
    }

    partial void OnIsConnectedChanged(bool value)
    {
        _ = value;

        if (_isShuttingDown)
        {
            return;
        }

        SafeNotifyCanExecuteChanged(() => AcknowledgeOperatingStateCommand.NotifyCanExecuteChanged());
        if (SuppressOperatingStateRecompute) return;
        RecomputeOperatingState();
    }

    partial void OnRestApiStatusTextChanged(string value)
    {
        _ = value;
        if (SuppressOperatingStateRecompute) return;
        RecomputeOperatingState();
    }

    partial void OnRestApiIsReachableChanged(bool value)
    {
        _ = value;
        if (SuppressOperatingStateRecompute) return;
        RecomputeOperatingState();
    }

    partial void OnSpeechHealthStatusChanged(string value)
    {
        _ = value;
        if (SuppressOperatingStateRecompute) return;
        RecomputeOperatingState();
    }

    partial void OnIsPostStartupInitializationRunningChanged(bool value)
    {
        _ = value;
        if (SuppressOperatingStateRecompute) return;
        RecomputeOperatingState();
    }

    partial void OnPostStartupStatusTextChanged(string value)
    {
        _ = value;
        if (SuppressOperatingStateRecompute) return;
        RecomputeOperatingState();
    }

    partial void OnIsTrackPowerOnChanged(bool value)
    {
        _ = value;
        if (SuppressOperatingStateRecompute) return;
        RecomputeOperatingState();
    }

    partial void OnIsOperatorAckRequiredChanged(bool value)
    {
        _ = value;

        if (_isShuttingDown)
        {
            return;
        }

        SafeNotifyCanExecuteChanged(() => AcknowledgeOperatingStateCommand.NotifyCanExecuteChanged());
    }

    partial void OnIsOperationalControlEnabledChanged(bool value)
    {
        _ = value;

        if (_isShuttingDown)
        {
            return;
        }

        SafeNotifyCanExecuteChanged(() => SetTrackPowerCommand.NotifyCanExecuteChanged());
    }

    /// <summary>
    /// Recomputes the shell-wide operating state from subsystem status.
    /// </summary>
    private void RecomputeOperatingState()
    {
        if (_isShuttingDown)
        {
            return;
        }

        OperatingStateKind nextState;
        string nextText;
        string nextDetail;
        string nextGlyph;
        bool nextShowInfoBar = false;
        string nextInfoBarTitle;
        string nextInfoBarMessage;
        bool nextFailSafe = false;

        if (!IsConnected)
        {
            if (_isManualDisconnectRequested)
            {
                nextState = OperatingStateKind.Recovering;
                nextText = "Offline";
                nextDetail = "Z21 manually disconnected.";
                nextGlyph = "\uE711";
            }
            else if (_isZ21Connecting || !_hasSeenSuccessfulZ21Connection || IsPostStartupInitializationRunning)
            {
                nextState = OperatingStateKind.Recovering;
                nextText = "Recovering";
                nextDetail = string.IsNullOrWhiteSpace(StatusText)
                    ? "Connecting to the Z21..."
                    : StatusText;
                nextGlyph = "\uE895";
            }
            else
            {
                nextState = OperatingStateKind.FailSafe;
                nextText = "Fail-Safe Active";
                nextDetail = string.IsNullOrWhiteSpace(_lastFailSafeReason)
                    ? "Unexpected loss of the Z21 connection."
                    : _lastFailSafeReason;
                nextGlyph = "\uE7BA";
                nextShowInfoBar = true;
                nextFailSafe = true;
            }
        }
        else if (IsOperatorAckRequired)
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

            if (_isEmergencyStopActive)
                issues.Add("Emergency stop active");

            if (_isShortCircuitActive)
                issues.Add("Short circuit detected");

            if (_isProgrammingModeActive)
                issues.Add("Programming mode active");

            if (_settings.Application.AutoStartWebApp && !RestApiIsReachable)
                issues.Add("REST API not reachable");

            if (SpeechHealthStatus.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                issues.Add("Azure Speech unavailable");

            if (issues.Count > 0)
            {
                nextState = OperatingStateKind.Degraded;
                nextText = "Degraded";
                nextDetail = string.Join(" | ", issues);
                nextGlyph = "\uE7BA";
                nextShowInfoBar = _settings.Application.AutoStartWebApp && !RestApiIsReachable;
            }
            else
            {
                nextState = OperatingStateKind.Normal;
                nextText = "Normal";
                nextDetail = IsTrackPowerOn
                    ? "Z21 connected, track power on."
                    : "Z21 connected.";
                nextGlyph = "\uE73E";
            }
        }

        nextInfoBarTitle = nextState switch
        {
            OperatingStateKind.FailSafe => "Fail-safe mode active",
            OperatingStateKind.Degraded => "Restricted operation",
            OperatingStateKind.Recovering => "Recovery in progress",
            _ => "Normal operation"
        };

        nextInfoBarMessage = nextState switch
        {
            OperatingStateKind.FailSafe => $"{nextDetail} Critical control actions remain blocked until the system is released again.",
            OperatingStateKind.Degraded => $"{nextDetail} The shell remains usable, but one or more supporting services are degraded.",
            OperatingStateKind.Recovering or OperatingStateKind.Normal => nextDetail,
            _ => nextDetail,
        };

        OperatingState = nextState;
        OperatingStateText = nextText;
        OperatingStateDetailText = nextDetail;
        OperatingStateIconGlyph = nextGlyph;
        HasOperatingStateDetailText = !string.IsNullOrWhiteSpace(nextDetail);
        ShowOperatingStateInfoBar = nextShowInfoBar;
        OperatingStateInfoBarTitle = nextInfoBarTitle;
        OperatingStateInfoBarMessage = nextInfoBarMessage;
        IsFailSafeActive = nextFailSafe;
        IsOperationalControlEnabled = IsConnected && !nextFailSafe && nextState != OperatingStateKind.Recovering;

        OperatingStateTooltipText = _lastFailSafeAt.HasValue && nextFailSafe
            ? $"{nextInfoBarMessage} Last fail-safe trigger: {_lastFailSafeAt.Value:HH:mm:ss}"
            : nextInfoBarMessage;
    }
}
