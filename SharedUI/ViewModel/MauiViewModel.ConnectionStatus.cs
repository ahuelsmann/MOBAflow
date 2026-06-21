// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

/// <summary>
/// Precomputed connection indicator display values for MAUI bindings (avoids repeated BoolToObjectConverter evaluation).
/// </summary>
public sealed partial class MauiViewModel
{
    public string Z21StatusText => IsConnected ? "ON" : "OFF";

    public string Z21StatusSemanticDescription => IsConnected ? "Z21 connected" : "Z21 disconnected";

    public string Z21IndicatorResourceKey => IsConnected ? "RailwayAccent" : "RailwayDanger";

    public string RestApiStatusText =>
        !IsMobaflowConnectionEnabled ? "OFF"
        : IsRestApiReachable ? "ON"
        : "Connecting";

    public string RestApiStatusSemanticDescription =>
        !IsMobaflowConnectionEnabled ? "MOBAflow connection disabled"
        : IsRestApiReachable ? "MOBAflow REST API connected"
        : "MOBAflow connection in progress";

    public string RestApiIndicatorResourceKey =>
        !IsMobaflowConnectionEnabled ? "RailwayDanger"
        : IsRestApiReachable ? "RailwayAccent"
        : "RailwayWarning";

    public string TrackPowerStatusText => IsTrackPowerOn ? "ON" : "OFF";

    public string TrackPowerStatusResourceKey => IsTrackPowerOn ? "RailwayWarning" : "RailwayDanger";

    partial void OnIsConnectedChanged(bool value)
    {
        if (value)
        {
            _shouldReconnectLocalZ21OnResume = true;
        }

        NotifyConnectionIndicatorProperties();
        UpdateRuntimeCoordinatorState();
        if (value && IsMobaflowConnectionEnabled && !IsRestApiReachable && !string.IsNullOrWhiteSpace(Z21IpAddress))
        {
            RunInBackground(DiscoverRestApiWithAnchorAsync(Z21IpAddress.Trim()), "REST discovery after Z21 connect");
        }
    }

    partial void OnIsRestApiReachableChanged(bool value)
    {
        OnPropertyChanged(nameof(RestApiStatusText));
        OnPropertyChanged(nameof(RestApiStatusSemanticDescription));
        OnPropertyChanged(nameof(RestApiIndicatorResourceKey));
        NotifySessionAvailabilityChanged();
        UpdateRuntimeCoordinatorState();
    }

    partial void OnIsTrackPowerOnChanged(bool value)
    {
        OnPropertyChanged(nameof(TrackPowerStatusText));
        OnPropertyChanged(nameof(TrackPowerStatusResourceKey));
    }

    private void NotifyConnectionIndicatorProperties()
    {
        OnPropertyChanged(nameof(Z21StatusText));
        OnPropertyChanged(nameof(Z21StatusSemanticDescription));
        OnPropertyChanged(nameof(Z21IndicatorResourceKey));
    }
}