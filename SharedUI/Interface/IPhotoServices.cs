// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

/// <summary>
/// Discovers a REST server that can be used by the application.
/// </summary>
public interface IRestDiscoveryService
{
    /// <summary>
    /// Attempts to discover a REST server endpoint via UDP multicast (or emulator shortcut).
    /// </summary>
    /// <returns>
    /// A task that returns the discovered IP address and port, or <c>null</c> values when discovery fails.
    /// </returns>
    Task<(string? ip, int? port)> DiscoverServerAsync();
}

/// <summary>
/// Discovers a Z21 command station on the local network (e.g. by scanning the subnet on port 21105).
/// </summary>
public interface IZ21DiscoveryService
{
    /// <summary>
    /// Attempts to discover a Z21 on the local network.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The IP address of the first responding Z21, or null if none found.</returns>
    Task<string?> DiscoverZ21Async(CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides operations for uploading photos to a remote server.
/// </summary>
public interface IPhotoUploadService
{
    /// <summary>
    /// Uploads a photo file to the configured server.
    /// </summary>
    /// <param name="serverIp">The server IP address.</param>
    /// <param name="serverPort">The server TCP port.</param>
    /// <param name="photoPath">The local file path of the photo to upload.</param>
    /// <param name="category">Logical category for the photo (for example, \"locomotives\").</param>
    /// <param name="entityId">The associated domain entity identifier.</param>
    /// <returns>
    /// A task that returns a tuple indicating whether the upload succeeded, the stored photo path when successful,
    /// and an optional error message when the upload fails.
    /// </returns>
    Task<(bool success, string? photoPath, string? error)> UploadPhotoAsync(string serverIp, int serverPort, string photoPath, string category, Guid entityId);

    /// <summary>
    /// Performs a simple health check against the photo upload endpoint.
    /// </summary>
    /// <param name="serverIp">The server IP address.</param>
    /// <param name="serverPort">The server TCP port.</param>
    /// <returns>A task that returns <c>true</c> when the server appears healthy; otherwise <c>false</c>.</returns>
    Task<bool> HealthCheckAsync(string serverIp, int serverPort);
}

/// <summary>
/// Captures photos from a local camera or device.
/// </summary>
public interface IPhotoCaptureService
{
    /// <summary>
    /// Captures a photo and returns the local file path, or <c>null</c> when the user cancels.
    /// </summary>
    /// <returns>
    /// A task that returns the local file path of the captured photo, or <c>null</c> if no photo was taken.
    /// </returns>
    Task<string?> CapturePhotoAsync();
}

/// <summary>
/// Registers the current client (e.g. MOBAsmart) with the REST API so it appears in the Overview "Connected clients" list.
/// </summary>
public interface IRestApiClientRegistration
{
    /// <summary>
    /// Registers this client with the server. Call when the app has discovered the API and health check succeeds.
    /// </summary>
    /// <param name="serverIp">REST API server IP.</param>
    /// <param name="serverPort">REST API server port.</param>
    /// <returns>True if registration succeeded.</returns>
    Task<bool> RegisterAsync(string serverIp, int serverPort);
}

/// <summary>
/// Null-object implementation of <see cref="IRestDiscoveryService"/> used when discovery is not available.
/// </summary>
public sealed class NullRestDiscoveryService : IRestDiscoveryService
{
    /// <inheritdoc />
    public Task<(string? ip, int? port)> DiscoverServerAsync() => Task.FromResult<(string?, int?)>((null, null));
}

/// <summary>
/// Null-object implementation of <see cref="IZ21DiscoveryService"/> used when Z21 discovery is not available.
/// </summary>
public sealed class NullZ21DiscoveryService : IZ21DiscoveryService
{
    /// <inheritdoc />
    public Task<string?> DiscoverZ21Async(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
}

/// <summary>
/// Null-object implementation of <see cref="IPhotoUploadService"/> used on platforms that do not support uploads.
/// </summary>
public sealed class NullPhotoUploadService : IPhotoUploadService
{
    /// <inheritdoc />
    public Task<(bool success, string? photoPath, string? error)> UploadPhotoAsync(string serverIp, int serverPort, string photoPath, string category, Guid entityId)
        => Task.FromResult<(bool, string?, string?)>((false, null, "Upload not supported on this platform"));

    /// <inheritdoc />
    public Task<bool> HealthCheckAsync(string serverIp, int serverPort) => Task.FromResult(false);
}

/// <summary>
/// Null-object implementation of <see cref="IPhotoCaptureService"/> used on platforms without camera support.
/// </summary>
public sealed class NullPhotoCaptureService : IPhotoCaptureService
{
    /// <inheritdoc />
    public Task<string?> CapturePhotoAsync() => Task.FromResult<string?>(null);
}

/// <summary>
/// Null-object implementation of <see cref="IRestApiClientRegistration"/> when registration is not available.
/// </summary>
public sealed class NullRestApiClientRegistration : IRestApiClientRegistration
{
    /// <inheritdoc />
    public Task<bool> RegisterAsync(string serverIp, int serverPort) => Task.FromResult(false);
}
