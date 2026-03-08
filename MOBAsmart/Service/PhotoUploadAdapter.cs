// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using SharedUI.Interface;

public class PhotoUploadAdapter : IPhotoUploadService
{
    private readonly PhotoUploadService _inner;
    public PhotoUploadAdapter(PhotoUploadService inner) => _inner = inner;

    public Task<(bool success, string? photoPath, string? error)> UploadPhotoAsync(string serverIp, int serverPort, string photoPath, string category, Guid entityId)
        => _inner.UploadPhotoAsync(serverIp, serverPort, photoPath, category, entityId);

    public Task<bool> HealthCheckAsync(string serverIp, int serverPort) => _inner.HealthCheckAsync(serverIp, serverPort);
}
