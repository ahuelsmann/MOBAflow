// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Runtime;

/// <summary>
/// Tab-aware update throttling for the MAUI mobile client.
/// </summary>
public sealed partial class MauiViewModel
{
    private bool _heavyUpdatesPaused;
    private bool _signalBoxTabActive;
    private IReadOnlyList<SignalBoxElementRuntimeSnapshot>? _pendingSignalBoxElements;

    /// <summary>
    /// Suppresses non-critical snapshot work (e.g. signal-box list rebuild) while another tab is active.
    /// </summary>
    public void PauseHeavyUpdates() => _heavyUpdatesPaused = true;

    /// <summary>
    /// Re-enables full snapshot updates and applies any deferred signal-box state once.
    /// </summary>
    public void ResumeHeavyUpdates()
    {
        _heavyUpdatesPaused = false;

        if (!_signalBoxTabActive)
        {
            return;
        }

        var elements = _pendingSignalBoxElements ?? _mobaRuntime.Current.SignalBoxElements;
        _pendingSignalBoxElements = null;
        RefreshSignalBoxElements(elements);
    }

    /// <summary>
    /// Tracks whether the SignalBox tab is visible so list rebuilds can be skipped on other tabs.
    /// </summary>
    public void SetSignalBoxTabActive(bool isActive)
    {
        _signalBoxTabActive = isActive;

        if (!isActive)
        {
            return;
        }

        if (_heavyUpdatesPaused || _pendingSignalBoxElements is not { } pending)
        {
            return;
        }

        _pendingSignalBoxElements = null;
        RefreshSignalBoxElements(pending);
    }
}
