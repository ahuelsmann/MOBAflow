// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Backend.Interface;
using Backend.Service.ProjectRuntimes;

using Common.Runtime;

using SharedUI.Interface;
using SharedUI.Service;

/// <summary>
/// Routes remote commands from MOBApi to the runtime of the project they name. Every project has its own runtime
/// and Z21, so a command never reaches another project; commands are recorded like local ones.
/// </summary>
public sealed class ProjectRuntimeCommandRouter(ProjectRuntimeHost host, IRecordingSessionService recordingSession)
{
    private readonly ProjectRuntimeHost _host = host ?? throw new ArgumentNullException(nameof(host));
    private readonly IRecordingSessionService _recordingSession =
        recordingSession ?? throw new ArgumentNullException(nameof(recordingSession));

    /// <summary>
    /// Gets the current snapshot of every project's runtime.
    /// </summary>
    public IReadOnlyList<MobaRuntimeSnapshot> Snapshots => [.. _host.Runtimes.Select(runtime => runtime.Runtime.Current)];

    /// <summary>
    /// Gets the command gateway of a project's runtime; null when the loaded solution has no such project.
    /// </summary>
    public IRuntimeCommandGateway? ForProject(Guid projectId) =>
        _host.Get(projectId)?.Runtime is { } runtime
            ? new RecordingRuntimeCommandGateway(new LocalRuntimeCommandGateway(runtime), _recordingSession)
            : null;
}
