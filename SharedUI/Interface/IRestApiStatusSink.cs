// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

using Moba.SharedUI.ViewModel;

/// <summary>
/// Receives REST API polling results and photo-upload assignments from WinUI services without coupling them to <see cref="MainWindowViewModel"/>.
/// </summary>
public interface IRestApiStatusSink
{
    /// <summary>
    /// Updates REST reachability, status text, and connected mobile clients (Overview UI).
    /// </summary>
    void UpdateRestApiStatus(string statusText, bool isReachable, IReadOnlyList<RestApiClientInfo>? clients);

    /// <summary>
    /// Assigns an uploaded photo file to the currently selected locomotive or wagon, if any.
    /// </summary>
    /// <returns>Which entity type was updated, if any.</returns>
    PhotoAssignmentTarget AssignUploadedPhotoToSelectedEntity(string photoPath);
}
