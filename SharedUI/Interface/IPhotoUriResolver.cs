// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

/// <summary>
/// Resolves relative photo paths to URIs that can be loaded by the UI (e.g. MOBApi file endpoint on MAUI).
/// </summary>
public interface IPhotoUriResolver
{
    /// <summary>
    /// Returns a loadable URI for the relative photo path, or <c>null</c> when unavailable.
    /// </summary>
    string? TryResolveRemoteUri(string? relativePhotoPath);
}
