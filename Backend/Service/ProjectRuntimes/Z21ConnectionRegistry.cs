// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service.ProjectRuntimes;

using Common.Events;

using Domain;

using Interface;

/// <summary>
/// Owns the Z21 connections for the application lifetime, one per Z21 address. A project runtime borrows the
/// connection of its project's Z21; a runtime created again for the same project takes over the open connection.
/// A Z21 already used by another project is not shared: the later runtime gets a detached connection that never
/// connects.
/// </summary>
/// <param name="busFactory">Creates the event bus of a new connection.</param>
/// <param name="z21Factory">Creates a Z21 client that publishes to the given bus.</param>
public sealed class Z21ConnectionRegistry(Func<ForwardingEventBus> busFactory, Func<IEventBus, IZ21> z21Factory)
    : IAsyncDisposable
{
    private readonly Func<ForwardingEventBus> _busFactory = busFactory ?? throw new ArgumentNullException(nameof(busFactory));
    private readonly Func<IEventBus, IZ21> _z21Factory = z21Factory ?? throw new ArgumentNullException(nameof(z21Factory));
    private readonly Dictionary<string, Entry> _connections = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _gate = new();

    /// <summary>
    /// Lends the connection of a project's Z21 to the project's runtime.
    /// </summary>
    public Z21ConnectionLease Acquire(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var key = ToKey(project.Z21);
        if (key is null)
        {
            return CreateDetached(project.Id, conflictProjectName: null);
        }

        lock (_gate)
        {
            if (_connections.TryGetValue(key, out var entry))
            {
                if (entry.OwnerProjectId is { } owner && owner != project.Id)
                {
                    return CreateDetached(project.Id, entry.OwnerProjectName);
                }

                entry.OwnerProjectId = project.Id;
                entry.OwnerProjectName = project.Name;
                return new Z21ConnectionLease(this, project.Id, key, entry.Z21, entry.EventBus, conflictProjectName: null);
            }

            var bus = _busFactory();
            var created = new Entry(_z21Factory(bus), bus) { OwnerProjectId = project.Id, OwnerProjectName = project.Name };
            _connections[key] = created;
            return new Z21ConnectionLease(this, project.Id, key, created.Z21, created.EventBus, conflictProjectName: null);
        }
    }

    /// <summary>
    /// Disconnects and removes every connection that no runtime borrows anymore.
    /// </summary>
    public async Task DisconnectUnusedAsync()
    {
        List<Entry> unused;
        lock (_gate)
        {
            unused = [.. _connections.Where(pair => pair.Value.OwnerProjectId is null).Select(pair => pair.Value)];
            foreach (var key in _connections.Where(pair => pair.Value.OwnerProjectId is null).Select(pair => pair.Key).ToList())
            {
                _connections.Remove(key);
            }
        }

        foreach (var entry in unused)
        {
            await CloseAsync(entry.Z21).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        List<Entry> all;
        lock (_gate)
        {
            all = [.. _connections.Values];
            _connections.Clear();
        }

        foreach (var entry in all)
        {
            await CloseAsync(entry.Z21).ConfigureAwait(false);
        }
    }

    internal void Release(Z21ConnectionLease lease)
    {
        if (lease.Key is null)
        {
            // A detached connection belongs to its runtime alone.
            lease.Z21.Dispose();
            return;
        }

        lock (_gate)
        {
            if (_connections.TryGetValue(lease.Key, out var entry) && entry.OwnerProjectId == lease.ProjectId)
            {
                // The connection stays open so that a runtime created again for this Z21 can take it over.
                entry.OwnerProjectId = null;
                entry.OwnerProjectName = null;
                entry.EventBus.IsForwarding = false;
            }
        }
    }

    /// <summary>Normalizes a Z21 address and port; null when no Z21 is assigned.</summary>
    public static string? ToKey(Z21Endpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        var address = endpoint.IpAddress.Trim();
        if (string.IsNullOrEmpty(address))
        {
            return null;
        }

        var port = endpoint.Port > 0 ? endpoint.Port : Z21Endpoint.DefaultPort;
        return $"{address}:{port}";
    }

    private Z21ConnectionLease CreateDetached(Guid projectId, string? conflictProjectName)
    {
        var bus = _busFactory();
        return new Z21ConnectionLease(this, projectId, key: null, _z21Factory(bus), bus, conflictProjectName);
    }

    private static async Task CloseAsync(IZ21 z21)
    {
        try
        {
            await z21.DisconnectAsync().ConfigureAwait(false);
        }
        finally
        {
            z21.Dispose();
        }
    }

    private sealed class Entry(IZ21 z21, ForwardingEventBus eventBus)
    {
        public IZ21 Z21 { get; } = z21;

        public ForwardingEventBus EventBus { get; } = eventBus;

        public Guid? OwnerProjectId { get; set; }

        public string? OwnerProjectName { get; set; }
    }
}

/// <summary>
/// The Z21 connection a project runtime borrows from the <see cref="Z21ConnectionRegistry"/>.
/// </summary>
public sealed class Z21ConnectionLease
{
    private readonly Z21ConnectionRegistry _registry;
    private int _released;

    internal Z21ConnectionLease(
        Z21ConnectionRegistry registry,
        Guid projectId,
        string? key,
        IZ21 z21,
        ForwardingEventBus eventBus,
        string? conflictProjectName)
    {
        _registry = registry;
        ProjectId = projectId;
        Key = key;
        Z21 = z21;
        EventBus = eventBus;
        ConflictProjectName = conflictProjectName;
    }

    /// <summary>Gets the project that borrows the connection.</summary>
    public Guid ProjectId { get; }

    /// <summary>Gets the normalized Z21 address; null for a detached connection.</summary>
    public string? Key { get; }

    /// <summary>Gets the Z21 client.</summary>
    public IZ21 Z21 { get; }

    /// <summary>Gets the event bus of the connection.</summary>
    public ForwardingEventBus EventBus { get; }

    /// <summary>Gets the project that already uses this Z21, when the runtime must not connect.</summary>
    public string? ConflictProjectName { get; }

    /// <summary>Returns the connection to the registry; an open shared connection stays open for a takeover.</summary>
    public void Release()
    {
        if (Interlocked.Exchange(ref _released, 1) == 0)
        {
            _registry.Release(this);
        }
    }
}
