// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

public interface IRestDiscoveryService
{
    Task<(string? ip, int? port)> DiscoverServerAsync();
}

public interface IPhotoUploadService
{
    Task<(bool success, string? photoPath, string? error)> UploadPhotoAsync(string serverIp, int serverPort, string photoPath, string category, Guid entityId);
    Task<bool> HealthCheckAsync(string serverIp, int serverPort);
}

public interface IPhotoCaptureService
{
    /// <summary>Captures a photo and returns a local file path, or null if cancelled.</summary>
    Task<string?> CapturePhotoAsync();
}

public sealed class NullRestDiscoveryService : IRestDiscoveryService
{
    public Task<(string? ip, int? port)> DiscoverServerAsync() => Task.FromResult<(string?, int?)>((null, null));
}

public sealed class NullPhotoUploadService : IPhotoUploadService
{
    public Task<(bool success, string? photoPath, string? error)> UploadPhotoAsync(string serverIp, int serverPort, string photoPath, string category, Guid entityId)
        => Task.FromResult<(bool, string?, string?)>((false, null, "Upload not supported on this platform"));

    public Task<bool> HealthCheckAsync(string serverIp, int serverPort) => Task.FromResult(false);
}

public sealed class NullPhotoCaptureService : IPhotoCaptureService
{
    public Task<string?> CapturePhotoAsync() => Task.FromResult<string?>(null);
}
