// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Interface;
using Common.Extension;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Interface;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.ComponentModel;

/// <summary>Projects one cancellable, read-only locomotive usage query into the existing Messages area.</summary>
public sealed partial class LocomotiveTrainUsageViewModel : ObservableObject, IDisposable
{
    private readonly ISolutionSession _session;
    private readonly ILocomotiveTrainReferenceAnalyzer _analyzer;
    private readonly ILocomotiveTrainRuntimeUsageReader _reader;
    private readonly IProjectRuntimeSnapshots _snapshots;
    private readonly IUiDispatcher _dispatcher;
    private readonly ILogger<LocomotiveTrainUsageViewModel> _logger;
    private QueryContext? _context;
    private Project? _targetProject;
    private Guid? _targetId;
    private bool _disposed;
    private static readonly Action<ILogger, Guid, Exception?> LogQueryFailed =
        LoggerMessage.Define<Guid>(LogLevel.Error, new EventId(1, nameof(LogQueryFailed)),
            "Reading locomotive train usage failed for project {ProjectId}");
    private static readonly Action<ILogger, Exception?> LogReceiptCheckFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(2, nameof(LogReceiptCheckFailed)),
            "Checking locomotive train usage receipt failed");
    private static readonly Action<ILogger, Guid, Exception?> LogRevalidationFailed =
        LoggerMessage.Define<Guid>(LogLevel.Error, new EventId(3, nameof(LogRevalidationFailed)),
            "Revalidating locomotive train usage failed for project {ProjectId}");

    /// <summary>Uses the existing session, owner reader and UI dispatcher; it owns no runtime or model persistence.</summary>
    public LocomotiveTrainUsageViewModel(
        ISolutionSession session,
        ILocomotiveTrainReferenceAnalyzer analyzer,
        ILocomotiveTrainRuntimeUsageReader reader,
        IProjectRuntimeSnapshots snapshots,
        IUiDispatcher dispatcher,
        ILogger<LocomotiveTrainUsageViewModel> logger)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _session.SolutionReplacing += OnSolutionLoad;
        _session.SolutionLoaded += OnSolutionLoad;
        _session.ModelChanged += OnModelChanged;
        _session.PropertyChanged += OnSessionPropertyChanged;
        _snapshots.RuntimeSnapshotChanged += OnRuntimeSnapshotChanged;
    }

    /// <summary>Copied current editor evidence, cleared when its query context is invalidated.</summary>
    [ObservableProperty]
    public partial LocomotiveTrainReferenceResult? EditedResult { get; set; }

    /// <summary>Revalidated runtime evidence, independent of the editor source.</summary>
    [ObservableProperty]
    public partial LocomotiveTrainRuntimeObservation? RuntimeObservation { get; set; }

    /// <summary>Source-qualified copied occurrences for the active explicit query only.</summary>
    public ImmutableArray<LocomotiveTrainFinding> Diagnostics =>
        [.. (EditedResult?.Findings ?? []).Concat(RuntimeObservation?.Definition.Findings ?? [])];

    /// <summary>Always-visible source and A/B/C coverage; never an aggregate non-use or Delete decision.</summary>
    public string CoverageSummary => EditedResult is null ? string.Empty
        : $"Edited project data: {EditedResult.ProjectName} - {DescribeDefinition(EditedResult)}. "
          + (RuntimeObservation is { } runtime
              ? $"Loaded runtime definition (A): {runtime.Definition.ProjectName} - {DescribeDefinition(runtime.Definition)}; "
                + $"{(runtime.LoadedDefinitionCoverage.State == LocomotiveTrainCoverageState.Complete ? "observed" : "checked")} {runtime.ObservedAtUtc:O}. "
              : "Loaded runtime definition (A): Unknown (query pending). ")
          + "Retained definitions (B): Unknown - Not checked in this slice. Operational use (C): Unknown - Not checked in this slice.";

    private static string DescribeDefinition(LocomotiveTrainReferenceResult result) =>
        result.Coverage.State == LocomotiveTrainCoverageState.Unknown
            ? $"Unknown ({ReasonLabel(result.Coverage.Reason)})"
            : $"Complete; target {result.TargetState}; "
              + (result.Findings.IsEmpty
                  ? result.Source == LocomotiveTrainSource.LoadedRuntimeDefinition
                      ? "No train references found in this loaded definition"
                      : "No train references found in edited project data"
                  : $"{result.Findings.Length} train reference(s)");

    private static string ReasonLabel(LocomotiveTrainCoverageReason reason) => reason switch
    {
        LocomotiveTrainCoverageReason.OwnerUnavailable => "Owner unavailable",
        LocomotiveTrainCoverageReason.ContextMismatch => "Loaded project context does not match",
        LocomotiveTrainCoverageReason.UnsupportedRemote => "Remote usage query is not supported",
        LocomotiveTrainCoverageReason.QueryFailed => "Query failed",
        LocomotiveTrainCoverageReason.InvalidIdentity => "Invalid or duplicated train or locomotive identity",
        LocomotiveTrainCoverageReason.StaleObservation => "Definition changed; refresh required",
        _ => "Not checked in this slice"
    };

    /// <summary>Forwards the current locomotive-page selection without changing it or reading a runtime.</summary>
    public void SetTarget(Project? project, Guid? locomotiveId)
    {
        if (ReferenceEquals(_targetProject, project) && _targetId == locomotiveId)
            return;
        Invalidate();
        _targetProject = project;
        _targetId = locomotiveId;
        ShowUsageCommand.NotifyCanExecuteChanged();
    }

    private bool CanShowUsage() => !_disposed && _targetProject is not null && _targetId is { } id && id != Guid.Empty
        && _session.Solution.Projects.Contains(_targetProject) && _targetProject.Locomotives.Any(locomotive => locomotive.Id == id);

    [RelayCommand(CanExecute = nameof(CanShowUsage))]
    private Task ShowUsageAsync(CancellationToken cancellationToken) =>
        _targetProject is { } project ? QueryAsync(project, _targetId, cancellationToken) : Task.CompletedTask;

    /// <summary>Reads any project in this loaded solution on the established model thread, including non-selected projects.</summary>
    public Task QueryAsync(Project project, Guid? locomotiveId, CancellationToken cancellationToken = default) =>
        _dispatcher.InvokeOnUiAsync(() => QueryCoreAsync(project, locomotiveId, cancellationToken));

    private async Task QueryCoreAsync(Project project, Guid? locomotiveId, CancellationToken cancellationToken)
    {
        Invalidate();
        if (_disposed || cancellationToken.IsCancellationRequested || !_session.Solution.Projects.Contains(project)
            || project.Id == Guid.Empty || locomotiveId == Guid.Empty)
            return;

        var request = new LocomotiveTrainUsageRequest(Guid.NewGuid(), project, project.Id, locomotiveId);
        var context = new QueryContext(request, cancellationToken);
        _context = context;
        EditedResult = _analyzer.Analyze(project, LocomotiveTrainSource.EditedProjectData, locomotiveId);
        NotifyResultsChanged();
        try
        {
            var runtime = await _reader.ReadAsync(request, context.Token).ConfigureAwait(true);
            if (!IsActive(context))
                return;
            if (runtime.RequestId != request.RequestId || runtime.ProjectId != request.ProjectId)
                runtime = LocomotiveTrainRuntimeObservation.Unknown(request, LocomotiveTrainCoverageReason.ContextMismatch);
            if (runtime.LoadedDefinitionCoverage.State == LocomotiveTrainCoverageState.Complete)
            {
                var validity = await _reader.IsCurrentAsync(runtime, context.Token).ConfigureAwait(true);
                if (!IsActive(context))
                    return;
                if (validity != LocomotiveTrainObservationValidity.Current)
                    runtime = LocomotiveTrainRuntimeObservation.Unknown(request, validity == LocomotiveTrainObservationValidity.Stale
                        ? LocomotiveTrainCoverageReason.StaleObservation : LocomotiveTrainCoverageReason.OwnerUnavailable);
            }
            if (!IsActive(context))
                return;
            RuntimeObservation = runtime;
            NotifyResultsChanged();
        }
        catch (OperationCanceledException) when (context.Token.IsCancellationRequested)
        {
            if (ReferenceEquals(_context, context))
                Invalidate();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogQueryFailed(_logger, project.Id, exception);
            if (IsActive(context))
            {
                RuntimeObservation = LocomotiveTrainRuntimeObservation.Unknown(request, LocomotiveTrainCoverageReason.QueryFailed);
                NotifyResultsChanged();
            }
        }
    }

    private bool IsActive(QueryContext context) => !_disposed && ReferenceEquals(_context, context)
        && !context.Token.IsCancellationRequested && _session.Solution.Projects.Contains(context.Request.ExpectedProject);

    private bool IsContextCurrent(QueryContext context) =>
        !_disposed && ReferenceEquals(Volatile.Read(ref _context), context) && !context.Token.IsCancellationRequested;

    private void OnRuntimeSnapshotChanged(object? sender, ProjectRuntimeSnapshotEventArgs args)
    {
        var context = Volatile.Read(ref _context);
        if (context is not null && context.Request.ProjectId == args.Snapshot.ProjectId && IsContextCurrent(context))
            QueueReceiptCheck(context);
    }

    private void QueueReceiptCheck(QueryContext context)
    {
        Interlocked.Exchange(ref context.ReceiptCheckRequested, 1);
        if (!IsContextCurrent(context) || Interlocked.CompareExchange(ref context.ReceiptCheckPending, 1, 0) != 0)
            return;
        // All-project snapshot events are raw background callbacks, unlike the decorated UI EventBus.
        _dispatcher.InvokeOnUi(() => RevalidateReceiptAsync(context)
            .Observe(exception => LogReceiptCheckFailed(_logger, exception)));
    }

    private async Task RevalidateReceiptAsync(QueryContext context)
    {
        try
        {
            while (IsContextCurrent(context) && Interlocked.Exchange(ref context.ReceiptCheckRequested, 0) != 0)
            {
                var observation = RuntimeObservation;
                if (observation?.LoadedDefinitionCoverage.State != LocomotiveTrainCoverageState.Complete)
                    return;
                var validity = await _reader.IsCurrentAsync(observation, context.Token).ConfigureAwait(false);
                await _dispatcher.InvokeOnUiAsync(() =>
                {
                    if (IsActive(context) && ReferenceEquals(RuntimeObservation, observation)
                        && validity != LocomotiveTrainObservationValidity.Current)
                    {
                        RuntimeObservation = LocomotiveTrainRuntimeObservation.Unknown(context.Request,
                            validity == LocomotiveTrainObservationValidity.Stale
                                ? LocomotiveTrainCoverageReason.StaleObservation : LocomotiveTrainCoverageReason.OwnerUnavailable);
                        NotifyResultsChanged();
                    }
                    return Task.CompletedTask;
                }).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (context.Token.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogRevalidationFailed(_logger, context.Request.ProjectId, exception);
            await _dispatcher.InvokeOnUiAsync(() =>
            {
                if (IsActive(context))
                {
                    RuntimeObservation = LocomotiveTrainRuntimeObservation.Unknown(context.Request, LocomotiveTrainCoverageReason.QueryFailed);
                    NotifyResultsChanged();
                }
                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref context.ReceiptCheckPending, 0);
            if (IsContextCurrent(context) && Volatile.Read(ref context.ReceiptCheckRequested) != 0)
                QueueReceiptCheck(context);
        }
    }

    private void OnSolutionLoad(object? sender, EventArgs args) => Invalidate();

    private void OnModelChanged(object? sender, PropertyChangedEventArgs args) => Invalidate();

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(ISolutionSession.SelectedProject))
        {
            SetTarget(null, null);
            Invalidate();
        }
    }

    private void Invalidate()
    {
        var previous = _context;
        _context = null;
        previous?.Dispose();
        EditedResult = null;
        RuntimeObservation = null;
        NotifyResultsChanged();
    }

    private void NotifyResultsChanged()
    {
        OnPropertyChanged(nameof(Diagnostics));
        OnPropertyChanged(nameof(CoverageSummary));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _session.SolutionReplacing -= OnSolutionLoad;
        _session.SolutionLoaded -= OnSolutionLoad;
        _session.ModelChanged -= OnModelChanged;
        _session.PropertyChanged -= OnSessionPropertyChanged;
        _snapshots.RuntimeSnapshotChanged -= OnRuntimeSnapshotChanged;
        var previous = _context;
        _context = null;
        previous?.Dispose();
    }

    private sealed class QueryContext : IDisposable
    {
        private readonly CancellationTokenSource _cancellation;
        public QueryContext(LocomotiveTrainUsageRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Token = _cancellation.Token;
        }
        public LocomotiveTrainUsageRequest Request { get; }
        public CancellationToken Token { get; }
        public int ReceiptCheckRequested;
        public int ReceiptCheckPending;
        public void Dispose()
        {
            _cancellation.Cancel();
            _cancellation.Dispose();
        }
    }
}
