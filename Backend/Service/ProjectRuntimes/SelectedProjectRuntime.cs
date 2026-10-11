// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service.ProjectRuntimes;

using Common.Events;
using Common.Runtime;

using Domain;

using Interface;

using Interlocking;

using Model;

/// <summary>
/// The runtime surface the MOBAflow UI and host services use: commands and state of the selected project's
/// runtime. Project activation and editor updates go to the runtime of the named project.
/// </summary>
public sealed class SelectedProjectRuntime : IMobaRuntime
{
    private static readonly MobaRuntimeSnapshot NoProject = new() { StatusText = "No project selected" };

    private readonly ProjectRuntimeHost _host;
    private readonly IEventBus _applicationBus;

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectedProjectRuntime"/> class.
    /// </summary>
    public SelectedProjectRuntime(ProjectRuntimeHost host, IEventBus applicationBus)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _applicationBus = applicationBus ?? throw new ArgumentNullException(nameof(applicationBus));
        _host.SelectedRuntimeChanged += OnSelectedRuntimeChanged;
    }

    /// <inheritdoc />
    public MobaRuntimeSnapshot Current => Selected?.Current ?? NoProject;

    private IMobaRuntime? Selected => _host.Selected?.Runtime;

    /// <summary>Project runtimes start when the session creates them.</summary>
    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task ActivateProjectAsync(Project editableProject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(editableProject);
        return _host.Get(editableProject.Id)?.Runtime.ActivateProjectAsync(editableProject, cancellationToken)
            ?? Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateProjectAsync(Project editableProject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(editableProject);
        return _host.UpdateAsync(editableProject, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken = default) =>
        Selected?.ConnectAsync(cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default) =>
        Selected?.DisconnectAsync(cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task RequestSystemStateAsync(CancellationToken cancellationToken = default) =>
        Selected?.RequestSystemStateAsync(cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task SetLocomotiveDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default) =>
        Selected?.SetLocomotiveDriveAsync(address, speed, forward, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task SetLocomotiveFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default) =>
        Selected?.SetLocomotiveFunctionAsync(address, functionIndex, isOn, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task SetAllLocomotiveFunctionsOffAsync(int address, CancellationToken cancellationToken = default) =>
        Selected?.SetAllLocomotiveFunctionsOffAsync(address, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task RequestLocomotiveInfoAsync(int address, CancellationToken cancellationToken = default) =>
        Selected?.RequestLocomotiveInfoAsync(address, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task SetTrackPowerAsync(bool isOn, CancellationToken cancellationToken = default) =>
        Selected?.SetTrackPowerAsync(isOn, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task AcknowledgeFailSafeAsync(CancellationToken cancellationToken = default) =>
        Selected?.AcknowledgeFailSafeAsync(cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task SimulateFeedbackAsync(int inPort, CancellationToken cancellationToken = default) =>
        Selected?.SimulateFeedbackAsync(inPort, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task ResetJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default) =>
        Selected?.ResetJourneyAsync(journeyId, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task ResetInPortCountersAsync(CancellationToken cancellationToken = default) =>
        Selected?.ResetInPortCountersAsync(cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task SetInPortCounterAsync(uint inPort, ulong value, CancellationToken cancellationToken = default) =>
        Selected?.SetInPortCounterAsync(inPort, value, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task ResetInPortCounterAsync(uint inPort, CancellationToken cancellationToken = default) =>
        Selected?.ResetInPortCounterAsync(inPort, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task SetSignalAspectAsync(Guid signalId, SignalAspect signalAspect, CancellationToken cancellationToken = default) =>
        Selected?.SetSignalAspectAsync(signalId, signalAspect, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task SendTurnoutCommandAsync(int decoderAddress, int output, bool activate, bool queue = false, CancellationToken cancellationToken = default) =>
        Selected?.SendTurnoutCommandAsync(decoderAddress, output, activate, queue, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public void SetSystemStatePollingInterval(int intervalSeconds)
    {
        foreach (var runtime in _host.Runtimes)
        {
            runtime.Runtime.SetSystemStatePollingInterval(intervalSeconds);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<Z21TrafficPacket> GetTrafficPackets() => Selected?.GetTrafficPackets() ?? [];

    /// <inheritdoc />
    public void ClearTrafficMonitor() => Selected?.ClearTrafficMonitor();

    private void OnSelectedRuntimeChanged(object? sender, EventArgs e)
    {
        _applicationBus.Publish(new SelectedRuntimeChangedEvent());
        _applicationBus.Publish(new RuntimeSnapshotChangedEvent(Current));
    }
}

/// <summary>
/// The interlocking of the selected project's runtime, for the interlocking controls of the MOBAflow UI.
/// </summary>
public sealed class SelectedProjectInterlocking(ProjectRuntimeHost host) : IInterlockingRuntime
{
    // The host owns the runtimes; this proxy only reads the selected one.
    private readonly Func<IInterlockingRuntime?> _selected = host is null
        ? throw new ArgumentNullException(nameof(host))
        : () => host.Selected?.Interlocking;

    private IInterlockingRuntime? Selected => _selected();

    /// <inheritdoc />
    public InterlockingRuntimeState Current => Selected?.Current ?? InterlockingRuntimeState.Empty;

    /// <inheritdoc />
    public bool IsSynchronized => Selected?.IsSynchronized ?? false;

    /// <summary>Each project runtime activates its own interlocking definition.</summary>
    public Task ActivateAsync(InterlockingDefinition definition, CancellationToken cancellationToken = default) =>
        Selected?.ActivateAsync(definition, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task SynchronizeAsync(CancellationToken cancellationToken = default) =>
        Selected?.SynchronizeAsync(cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task<TurnoutCoordinatorResult> SetTurnoutAsync(
        Guid turnoutId,
        TurnoutPosition position,
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        Selected?.SetTurnoutAsync(turnoutId, position, correlationId, cancellationToken)
        ?? throw new InvalidOperationException("No project is selected.");

    /// <inheritdoc />
    public Task WhenIdleAsync(CancellationToken cancellationToken = default) =>
        Selected?.WhenIdleAsync(cancellationToken) ?? Task.CompletedTask;

    /// <summary>The project runtimes own and dispose their interlocking.</summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
