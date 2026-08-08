// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

/// <summary>
/// Requests camera access before the MOBAsmart QR scanner becomes visible.
/// </summary>
public interface IPairingCameraAccess
{
    Task<bool> RequestAsync(CancellationToken cancellationToken = default);
}
