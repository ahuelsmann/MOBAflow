// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Service;

using SharedUI.Interface;

/// <summary>
/// Adapts the MAUI camera permission prompt to the shared pairing ViewModel.
/// </summary>
public sealed class PairingCameraPermission : IPairingCameraPermission
{
    public async Task<bool> RequestAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        cancellationToken.ThrowIfCancellationRequested();
        return status == PermissionStatus.Granted;
    }
}