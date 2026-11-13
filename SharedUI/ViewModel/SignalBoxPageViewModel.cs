// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Interface;

using Common.Events;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Contracts.Runtime;

using Domain;

using Interface;

using Service;

using System.ComponentModel;

/// <summary>
/// Owns the Signal Box page: the persisted toolbox and properties panel expansion, the ordered editor changes
/// (runtime configuration refresh, signal aspect, solution save) and the projection of runtime signal-box state
/// onto the edited plan of the selected project. The session owns the project selection; aspects go through the
/// runtime command gateway.
/// </summary>
public sealed partial class SignalBoxPageViewModel : ObservableObject, IDisposable
{
    private readonly ISolutionSession _session;
    private readonly IConnectionRuntime _runtimeConnection;
    private readonly IRuntimeCommandGateway _runtimeCommandGateway;
    private readonly IEventBus _eventBus;
    private readonly LayoutPersistenceService _layout;
    private readonly Guid _runtimeSnapshotSubscription;
    private bool _isSignalBoxToolboxExpanded;
    private bool _isSignalBoxPropertiesExpanded;
    private bool _disposed;

    // Signal-box editor changes run one after another so an aspect never overtakes a configuration update.
    private Task _signalBoxChanges = Task.CompletedTask;

    /// <summary>Creates the Signal Box page state and applies the current runtime signal-box state.</summary>
    /// <param name="session">Owns the project selection and saves the solution.</param>
    /// <param name="runtimeConnection">Refreshes the runtime's copy of the edited project.</param>
    /// <param name="runtimeCommandGateway">Sends signal aspects (local, recorded or remote).</param>
    /// <param name="runtimeSnapshots">Provides the runtime state at construction.</param>
    /// <param name="eventBus">Delivers runtime snapshot changes on the UI thread.</param>
    /// <param name="layout">Persists the toolbox and properties panel expansion.</param>
    public SignalBoxPageViewModel(
        ISolutionSession session,
        IConnectionRuntime runtimeConnection,
        IRuntimeCommandGateway runtimeCommandGateway,
        IRuntimeSnapshotProvider runtimeSnapshots,
        IEventBus eventBus,
        LayoutPersistenceService layout)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(runtimeConnection);
        ArgumentNullException.ThrowIfNull(runtimeCommandGateway);
        ArgumentNullException.ThrowIfNull(runtimeSnapshots);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(layout);
        _session = session;
        _runtimeConnection = runtimeConnection;
        _runtimeCommandGateway = runtimeCommandGateway;
        _eventBus = eventBus;
        _layout = layout;
        _isSignalBoxToolboxExpanded = layout.Layout.SignalBoxPage.IsToolboxExpanded;
        _isSignalBoxPropertiesExpanded = layout.Layout.SignalBoxPage.IsPropertiesExpanded;
        _session.PropertyChanged += OnSessionPropertyChanged;
        _runtimeSnapshotSubscription = eventBus.Subscribe<RuntimeSnapshotChangedEvent>(OnRuntimeSnapshotChanged);
        ApplyRuntimeSnapshot(runtimeSnapshots.Current);
    }

    /// <summary>Raised when runtime signal-box state was projected onto the selected project's plan.</summary>
    public event EventHandler? SignalBoxRuntimeStateChanged;

    /// <summary>Raised after the session loaded a solution.</summary>
    public event EventHandler? SolutionLoaded
    {
        add => _session.SolutionLoaded += value;
        remove => _session.SolutionLoaded -= value;
    }

    /// <summary>Gets the selected project whose signal-box plan the page edits; the session owns the selection.</summary>
    public ProjectViewModel? SelectedProject => _session.SelectedProject;

    /// <summary>Gets an actionable, non-modal description of the current persistence state.</summary>
    public string SolutionSaveStatusText => _session.SolutionSaveStatusText;

    public bool IsSignalBoxToolboxExpanded
    {
        get => _isSignalBoxToolboxExpanded;
        set
        {
            if (SetProperty(ref _isSignalBoxToolboxExpanded, value))
                _layout.Persist(layout => layout.SignalBoxPage.IsToolboxExpanded = value);
        }
    }

    public bool IsSignalBoxPropertiesExpanded
    {
        get => _isSignalBoxPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isSignalBoxPropertiesExpanded, value))
                _layout.Persist(layout => layout.SignalBoxPage.IsPropertiesExpanded = value);
        }
    }

    [RelayCommand]
    private void OpenSignalBoxWorkbench() => IsSignalBoxPropertiesExpanded = true;

    /// <summary>
    /// Sends the signal's current aspect through the runtime command gateway.
    /// The runtime resolves the signal by id in the active project.
    /// </summary>
    /// <param name="signal">The signal whose <see cref="SbSignal.SignalAspect"/> is sent.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task SetSignalAspectAsync(SbSignal signal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signal);
        return _runtimeCommandGateway.SetSignalAspectAsync(signal.Id, signal.SignalAspect, cancellationToken);
    }

    /// <summary>
    /// Applies a property change made in the signal-box editor: updates the runtime's signal-box configuration,
    /// sends the signal aspect when requested and persists the solution. Changes run in call order.
    /// </summary>
    /// <param name="element">The changed signal-box element.</param>
    /// <param name="requiresPersistence">Whether the change alters stored configuration.</param>
    /// <param name="requiresSignalCommand">Whether the change requests a signal aspect on the layout.</param>
    public Task ApplySignalBoxElementChangeAsync(SbElement element, bool requiresPersistence, bool requiresSignalCommand)
    {
        ArgumentNullException.ThrowIfNull(element);
        var change = ApplySignalBoxElementChangeAfterAsync(
            _signalBoxChanges,
            element,
            requiresPersistence,
            SelectedProject?.Model,
            requiresSignalCommand);
        _signalBoxChanges = change;
        return change;
    }

    private async Task ApplySignalBoxElementChangeAfterAsync(
        Task previousChange,
        SbElement element,
        bool requiresPersistence,
        Project? project,
        bool requiresSignalCommand)
    {
        // A failed earlier change was reported to its own caller; it must not block later changes. Each step returns
        // to the UI context: the runtime copy and the save snapshot read the editor model, which the UI thread changes.
        await Task.WhenAny(previousChange).ConfigureAwait(true);

        if (requiresPersistence && project is not null)
        {
            // Refresh the runtime's project snapshot so the aspect uses the edited configuration.
            await _runtimeConnection.UpdateProjectAsync(project).ConfigureAwait(true);
        }

        if (requiresSignalCommand && element is SbSignal signal)
        {
            await SetSignalAspectAsync(signal).ConfigureAwait(true);
        }

        if (requiresPersistence)
        {
            await _session.SaveSolutionInternalAsync().ConfigureAwait(true);
        }
    }

    private void OnRuntimeSnapshotChanged(RuntimeSnapshotChangedEvent e) => ApplyRuntimeSnapshot(e.Snapshot);

    private void ApplyRuntimeSnapshot(MobaRuntimeSnapshot snapshot)
    {
        if (_disposed)
        {
            return;
        }

        if (SignalBoxRuntimeSync.ApplyToPlan(SelectedProject?.Model.SignalBoxPlan, snapshot.SignalBoxElements))
        {
            SignalBoxRuntimeStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ISolutionSession.SelectedProject):
                OnPropertyChanged(nameof(SelectedProject));
                break;
            case nameof(ISolutionSession.SolutionSaveStatusText):
                OnPropertyChanged(nameof(SolutionSaveStatusText));
                break;
        }
    }

    /// <summary>Stops following the session and the runtime once shutdown is confirmed.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session.PropertyChanged -= OnSessionPropertyChanged;
        _eventBus.Unsubscribe(_runtimeSnapshotSubscription);
    }
}
