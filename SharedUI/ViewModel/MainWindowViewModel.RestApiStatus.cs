// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Events;

using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.Extensions.Logging;

using System.Collections.ObjectModel;

/// <summary>
/// MainWindowViewModel - REST API status and connected clients for Overview page.
/// Receives updates via EventBus (RestApiStatusChangedEvent) marshalled by UiThreadEventBusDecorator.
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
        RestApiStatusText = statusText;
        RestApiIsReachable = isReachable;

        RestApiConnectedClients = clients is null
            ? []
            : new ObservableCollection<RestApiClientInfo>(clients);

        RestApiConnectedClientsEmpty = RestApiConnectedClients.Count == 0;

        RecomputeOperatingState();
    }

    /// <summary>
    /// Event handler for RestApiStatusChangedEvent.
    /// Called via UiThreadEventBusDecorator (already on UI thread).
    /// </summary>
    private void OnRestApiStatusChanged(RestApiStatusChangedEvent e)
    {
        UpdateRestApiStatus(e.Status, e.IsReachable, e.Clients);
    }

    /// <summary>
    /// Event handler for PhotoAssignedEvent.
    /// Called via UiThreadEventBusDecorator (already on UI thread).
    /// Performs photo assignment and logging.
    /// </summary>
    private void OnPhotoAssigned(PhotoAssignedEvent e)
    {
        var target = AssignUploadedPhotoToSelectedEntity(e.PhotoPath);

        // Log based on actual assignment result
        switch (target)
        {
            case PhotoAssignmentTarget.Locomotive:
                _logger.LogInformation("Assigned uploaded photo to selected locomotive: {PhotoPath}", e.PhotoPath);
                break;
            case PhotoAssignmentTarget.PassengerWagon:
                _logger.LogInformation("Assigned uploaded photo to selected passenger wagon: {PhotoPath}", e.PhotoPath);
                break;
            case PhotoAssignmentTarget.GoodsWagon:
                _logger.LogInformation("Assigned uploaded photo to selected goods wagon: {PhotoPath}", e.PhotoPath);
                break;
            default:
                _logger.LogDebug("Photo uploaded but no locomotive/wagon is selected. Path: {PhotoPath}", e.PhotoPath);
                break;
        }
    }
}