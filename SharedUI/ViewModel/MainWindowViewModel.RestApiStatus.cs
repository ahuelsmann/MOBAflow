// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// MainWindowViewModel - REST API status and connected clients for Overview page.
/// Updated by WinUI RestApiStatusService when polling http://127.0.0.1:RestApiPort/api/status.
/// </summary>
public partial class MainWindowViewModel
{
    /// <summary>
    /// Human-readable REST API status (e.g. "Running on port 5001" or "Not reachable").
    /// </summary>
    [ObservableProperty]
    private string _restApiStatusText = "—";

    /// <summary>
    /// True when the REST API (WebApp) is reachable at the configured port.
    /// </summary>
    [ObservableProperty]
    private bool _restApiIsReachable;

    /// <summary>
    /// List of clients currently connected to the REST API (e.g. MAUI app).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<RestApiClientInfo> _restApiConnectedClients = [];

    /// <summary>
    /// True when no clients are connected (for showing "No clients connected" hint in Overview).
    /// </summary>
    [ObservableProperty]
    private bool _restApiConnectedClientsEmpty = true;

    /// <summary>
    /// Updates REST API status and connected clients from the result of GET /api/status.
    /// Call from WinUI after fetching status (e.g. RestApiStatusService).
    /// </summary>
    public void UpdateRestApiStatus(string statusText, bool isReachable, IReadOnlyList<RestApiClientInfo>? clients)
    {
        RestApiStatusText = statusText ?? "—";
        RestApiIsReachable = isReachable;

        RestApiConnectedClients = clients is null
            ? []
            : new ObservableCollection<RestApiClientInfo>(clients);

        RestApiConnectedClientsEmpty = RestApiConnectedClients.Count == 0;
    }
}
