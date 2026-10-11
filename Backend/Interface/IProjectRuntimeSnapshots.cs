// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Interface;

using Common.Runtime;

/// <summary>
/// The snapshots of all project runtimes, for services that publish every project, such as the MOBApi host push.
/// </summary>
public interface IProjectRuntimeSnapshots
{
    /// <summary>Raised with each snapshot of any project's runtime; the snapshot names its project.</summary>
    event EventHandler<ProjectRuntimeSnapshotEventArgs>? RuntimeSnapshotChanged;

    /// <summary>Gets the current snapshot of every project's runtime.</summary>
    IReadOnlyList<MobaRuntimeSnapshot> Snapshots { get; }
}

/// <summary>
/// A new snapshot of one project's runtime; the snapshot names its project.
/// </summary>
public sealed class ProjectRuntimeSnapshotEventArgs(MobaRuntimeSnapshot snapshot) : EventArgs
{
    /// <summary>Gets the snapshot.</summary>
    public MobaRuntimeSnapshot Snapshot { get; } = snapshot;
}
