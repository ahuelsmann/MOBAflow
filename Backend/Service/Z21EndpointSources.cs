// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Common.Configuration;
using Common.Discovery;

using Domain;

using Interface;

using Protocol;

using System.Net;

/// <summary>
/// The Z21 address from the app settings, searched on the network when none is set or connections keep failing.
/// Used by MOBAsmart, which controls one Z21 directly.
/// </summary>
public sealed class SettingsZ21EndpointSource(AppSettings settings, IZ21DiscoveryService? discovery = null) : IZ21EndpointSource
{
    private readonly AppSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly IZ21DiscoveryService _discovery = discovery ?? new NullZ21DiscoveryService();

    /// <inheritdoc />
    public string DisplayAddress => _settings.Z21.CurrentIpAddress;

    /// <inheritdoc />
    public async Task<Z21EndpointResolution> ResolveAsync(bool rediscover, CancellationToken cancellationToken = default)
    {
        var configured = _settings.Z21.CurrentIpAddress?.Trim();
        if (string.IsNullOrEmpty(configured) || rediscover)
        {
            var discovered = await _discovery
                .DiscoverZ21Async(string.IsNullOrEmpty(configured) ? null : configured, cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(discovered))
            {
                _settings.Z21.CurrentIpAddress = discovered.Trim();
            }
        }

        var address = _settings.Z21.CurrentIpAddress?.Trim();
        if (string.IsNullOrEmpty(address))
        {
            return Z21EndpointResolution.Unavailable("No Z21 found on LAN");
        }

        if (!IPAddress.TryParse(address, out var ipAddress))
        {
            return Z21EndpointResolution.Unavailable($"Invalid Z21 IP address '{address}'");
        }

        var port = int.TryParse(_settings.Z21.DefaultPort, out var parsedPort) && parsedPort > 0
            ? parsedPort
            : Z21Protocol.DefaultPort;
        return new Z21EndpointResolution(ipAddress, port, null);
    }
}

/// <summary>
/// The Z21 assigned to a project. MOBAflow never searches the network on its own, because with several Z21 on
/// the network a search could connect a project to another layout.
/// </summary>
public sealed class ProjectZ21EndpointSource : IZ21EndpointSource
{
    private readonly Z21Endpoint _endpoint;
    private readonly string _projectName;
    private readonly string? _unavailableReason;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectZ21EndpointSource"/> class.
    /// </summary>
    /// <param name="project">The project whose Z21 the runtime uses.</param>
    /// <param name="unavailableReason">Why the runtime must not connect, for example a Z21 another project uses.</param>
    public ProjectZ21EndpointSource(Project project, string? unavailableReason = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        _endpoint = new Z21Endpoint { IpAddress = project.Z21.IpAddress, Port = project.Z21.Port };
        _projectName = project.Name;
        _unavailableReason = unavailableReason;
    }

    /// <inheritdoc />
    public string DisplayAddress => _endpoint.IpAddress;

    /// <inheritdoc />
    public Task<Z21EndpointResolution> ResolveAsync(bool rediscover, CancellationToken cancellationToken = default)
    {
        _ = rediscover;
        cancellationToken.ThrowIfCancellationRequested();
        if (_unavailableReason is not null)
        {
            return Task.FromResult(Z21EndpointResolution.Unavailable(_unavailableReason));
        }

        var address = _endpoint.IpAddress.Trim();
        if (string.IsNullOrEmpty(address))
        {
            return Task.FromResult(Z21EndpointResolution.Unavailable($"No Z21 assigned to project '{_projectName}'"));
        }

        if (!IPAddress.TryParse(address, out var ipAddress))
        {
            return Task.FromResult(Z21EndpointResolution.Unavailable($"Invalid Z21 IP address '{address}'"));
        }

        var port = _endpoint.Port > 0 ? _endpoint.Port : Z21Protocol.DefaultPort;
        return Task.FromResult(new Z21EndpointResolution(ipAddress, port, null));
    }
}
