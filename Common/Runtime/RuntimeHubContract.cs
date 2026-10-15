// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Runtime;

// The SignalR wire contract of the MOBApi runtime hub (/runtime-hub), shared by MOBApi, the MOBAflow host and
// MOBAsmart remotes. A method name is the wire name, so renaming a method changes the protocol on all sides.

/// <summary>
/// Hub methods the MOBAflow host calls: it owns the project runtimes and publishes their snapshots.
/// </summary>
public interface IRuntimeHubHostMethods
{
    /// <summary>Registers the calling connection as the host that executes remote commands.</summary>
    Task RegisterHost();

    /// <summary>Publishes the serialized snapshot of one project's runtime.</summary>
    Task PushSnapshot(string snapshotJson);
}

/// <summary>
/// Hub methods a MOBAsmart remote calls: it registers for one project and sends commands for that project's runtime.
/// </summary>
public interface IRuntimeHubRemoteMethods
{
    /// <summary>Registers a remote for a project; an empty <paramref name="projectId"/> means no project yet.</summary>
    Task RegisterRemote(string clientId, string projectId);

    Task SetSignalAspect(string projectId, string signalId, string aspect);

    Task SetLocomotiveDrive(string projectId, int address, int speed, bool forward);

    Task SetLocomotiveFunction(string projectId, int address, int functionIndex, bool isOn);
}

/// <summary>
/// Calls the hub sends to the MOBAflow host: validated remote commands for one project's runtime.
/// </summary>
public interface IRuntimeHubHostCallbacks
{
    Task ExecuteSetSignalAspect(string projectId, string signalId, string aspect);

    Task ExecuteSetLocomotiveDrive(string projectId, int address, int speed, bool forward);

    Task ExecuteSetLocomotiveFunction(string projectId, int address, int functionIndex, bool isOn);
}

/// <summary>
/// Calls the hub sends to MOBAsmart remotes: snapshots and session state of their project, and solution updates.
/// </summary>
public interface IRuntimeHubRemoteCallbacks
{
    Task SnapshotUpdated(string snapshotJson);

    /// <summary>Whether a host is connected and the project's runtime is connected to its Z21.</summary>
    Task SessionStateChanged(bool isOperational);

    /// <summary>The solution changed; <paramref name="updatedAtIso"/> is its round-trip ("O") timestamp.</summary>
    Task SolutionUpdated(string updatedAtIso);
}

/// <summary>
/// Every call the hub can send. Host and remotes connect to the same hub, and a strongly typed hub has exactly one
/// client type; each side handles only the callbacks of its own role.
/// </summary>
public interface IRuntimeHubCallbacks : IRuntimeHubHostCallbacks, IRuntimeHubRemoteCallbacks
{
    /// <summary>
    /// Sends <see cref="IRuntimeHubRemoteCallbacks.SolutionUpdated"/> and stops when <paramref name="cancellationToken"/>
    /// is cancelled. SignalR passes a trailing token to the send instead of the wire, so remotes receive one argument.
    /// </summary>
    Task SolutionUpdated(string updatedAtIso, CancellationToken cancellationToken);
}
