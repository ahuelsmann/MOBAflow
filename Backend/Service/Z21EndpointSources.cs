// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Domain;

using Interface;

using Protocol;

using System.Net;

/// <summary>
/// The Z21 assigned to a project. A runtime never searches the network on its own, because with several Z21 on the
/// network a search could connect a project to another layout. MOBAsmart passes the project selected on the phone,
/// so selecting another project changes the Z21.
/// </summary>
/// <param name="project">Returns the project whose Z21 the runtime uses; null when none is selected.</param>
/// <param name="unavailableReason">Why the runtime must not connect, for example a Z21 another project uses.</param>
public sealed class ProjectZ21EndpointSource(Func<Project?> project, string? unavailableReason = null) : IZ21EndpointSource
{
    private readonly Func<Project?> _project = project ?? throw new ArgumentNullException(nameof(project));
    private readonly string? _unavailableReason = unavailableReason;

    /// <summary>
    /// The Z21 the project has when its runtime is created (MOBAflow); a new assignment creates a new runtime.
    /// </summary>
    /// <param name="project">The project whose Z21 the runtime uses.</param>
    /// <param name="unavailableReason">Why the runtime must not connect, for example a Z21 another project uses.</param>
    public ProjectZ21EndpointSource(Project project, string? unavailableReason = null)
        : this(Fixed(project), unavailableReason)
    {
    }

    /// <inheritdoc />
    public string DisplayAddress => _project()?.Z21.IpAddress.Trim() ?? string.Empty;

    /// <inheritdoc />
    public Task<Z21EndpointResolution> ResolveAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_unavailableReason is not null)
        {
            return Task.FromResult(Z21EndpointResolution.Unavailable(_unavailableReason));
        }

        if (_project() is not { } selectedProject)
        {
            return Task.FromResult(Z21EndpointResolution.Unavailable("No project selected"));
        }

        var address = selectedProject.Z21.IpAddress.Trim();
        if (string.IsNullOrEmpty(address))
        {
            return Task.FromResult(Z21EndpointResolution.Unavailable($"No Z21 assigned to project '{selectedProject.Name}'"));
        }

        if (!IPAddress.TryParse(address, out var ipAddress))
        {
            return Task.FromResult(Z21EndpointResolution.Unavailable($"Invalid Z21 IP address '{address}'"));
        }

        var port = selectedProject.Z21.Port > 0 ? selectedProject.Z21.Port : Z21Protocol.DefaultPort;
        return Task.FromResult(new Z21EndpointResolution(ipAddress, port, null));
    }

    /// <summary>Copies the name and Z21 of a project, so later editor changes do not move the runtime.</summary>
    private static Func<Project?> Fixed(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var copy = new Project
        {
            Id = project.Id,
            Name = project.Name,
            Z21 = new Z21Endpoint { IpAddress = project.Z21.IpAddress, Port = project.Z21.Port }
        };
        return () => copy;
    }
}
