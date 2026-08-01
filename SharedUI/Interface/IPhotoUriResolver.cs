// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

/// <summary>
/// Opens relative photo paths for UI image loading without exposing transport credentials.
/// </summary>
public interface IPhotoUriResolver
{
    /// <summary>
    /// Opens the relative photo path, or returns <c>null</c> when unavailable.
    /// </summary>
    Task<Stream?> OpenReadAsync(
        string? relativePhotoPath,
        CancellationToken cancellationToken = default);
}
