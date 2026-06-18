// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

/// <summary>
/// Precomputed Z21 connection indicator display values for MAUI bindings.
/// </summary>
public sealed partial class TrainControlViewModel
{
    public string Z21StatusText => IsConnected ? "ON" : "OFF";

    public string Z21StatusSemanticDescription => IsConnected ? "Z21 connected" : "Z21 disconnected";

    public string Z21IndicatorResourceKey => IsConnected ? "RailwayAccent" : "RailwayDanger";

    public string DirectionStatusText => IsForward ? "Forward" : "Backward";

    partial void OnIsConnectedChanged(bool value)
    {
        OnPropertyChanged(nameof(Z21StatusText));
        OnPropertyChanged(nameof(Z21StatusSemanticDescription));
        OnPropertyChanged(nameof(Z21IndicatorResourceKey));
    }
}