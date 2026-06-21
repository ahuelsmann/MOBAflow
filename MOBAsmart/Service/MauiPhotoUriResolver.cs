// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Common.Configuration;
using Common.Path;

using SharedUI.Interface;

/// <summary>
/// Resolves locomotive and wagon photos against the connected MOBApi REST host.
/// </summary>
public sealed class MauiPhotoUriResolver : IPhotoUriResolver
{
    private readonly AppSettings _settings;

    public MauiPhotoUriResolver(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
    }

    /// <inheritdoc />
    public string? TryResolveRemoteUri(string? relativePhotoPath)
    {
        var ip = _settings.RestApi.CurrentIpAddress?.Trim();
        var port = _settings.RestApi.Port;
        return RemotePhotoUriBuilder.BuildHttpUri(ip, port, relativePhotoPath);
    }
}
