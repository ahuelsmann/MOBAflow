// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Interface;
using Backend.Service.Validation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Interface;
using System.Collections.ObjectModel;
using System.ComponentModel;

/// <summary>
/// Owns the state-based project diagnostics of the main window's Messages pane: the analyzed findings,
/// the severity filters and counts, the coverage summary of the locomotive usage query and the refresh
/// triggers. This is intentionally separate from the continuous application and Z21 logs on the Monitor page.
/// </summary>
public sealed partial class ProjectDiagnosticsViewModel : ObservableObject, IDisposable
{
    private readonly ISolutionSession _session;
    private readonly IProjectDiagnosticsService _projectDiagnosticsService;
    private readonly LocomotiveTrainUsageViewModel _usage;
    private bool _isDisposed;

    /// <param name="session">Owns the solution and the project selection; a project or persisted model change refreshes.</param>
    /// <param name="projectDiagnosticsService">Analyzes the selected project.</param>
    /// <param name="usage">The host-owned locomotive usage query whose findings join the pane.</param>
    public ProjectDiagnosticsViewModel(
        ISolutionSession session,
        IProjectDiagnosticsService projectDiagnosticsService,
        LocomotiveTrainUsageViewModel usage)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(projectDiagnosticsService);
        ArgumentNullException.ThrowIfNull(usage);

        _session = session;
        _projectDiagnosticsService = projectDiagnosticsService;
        _usage = usage;

        _usage.PropertyChanged += OnLocomotiveUsageChanged;
        _session.PropertyChanged += OnSessionPropertyChanged;
        _session.ModelChanged += OnSolutionModelChanged;
        RefreshProjectDiagnostics();
    }

    /// <summary>All findings of the last analysis, independent of the filters.</summary>
    public ObservableCollection<ProjectDiagnostic> ProjectDiagnostics { get; } = [];

    /// <summary>The findings that pass the severity filters.</summary>
    public ObservableCollection<ProjectDiagnostic> VisibleProjectDiagnostics { get; } = [];

    /// <summary>Opening the pane analyzes the selected project again.</summary>
    [ObservableProperty]
    public partial bool IsDiagnosticsExpanded { get; set; }

    [ObservableProperty]
    public partial bool ShowDiagnosticErrors { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowDiagnosticWarnings { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowDiagnosticInformation { get; set; } = true;

    public int DiagnosticErrorCount =>
        ProjectDiagnostics.Count(item => item.Severity == ProjectDiagnosticSeverity.Error);

    public int DiagnosticWarningCount =>
        ProjectDiagnostics.Count(item => item.Severity == ProjectDiagnosticSeverity.Warning);

    public int DiagnosticInformationCount =>
        ProjectDiagnostics.Count(item => item.Severity == ProjectDiagnosticSeverity.Information);

    public string DiagnosticsSummary =>
        $"{DiagnosticErrorCount} errors, {DiagnosticWarningCount} warnings, {DiagnosticInformationCount} information";

    /// <summary>Source coverage remains visible even when neutral Information rows are filtered out.</summary>
    public string LocomotiveUsageCoverageSummary => _usage.CoverageSummary;

    private void OnLocomotiveUsageChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(LocomotiveTrainUsageViewModel.Diagnostics))
        {
            if (_usage.EditedResult is not null)
                IsDiagnosticsExpanded = true;
            RefreshProjectDiagnostics();
        }
        if (args.PropertyName == nameof(LocomotiveTrainUsageViewModel.CoverageSummary))
            OnPropertyChanged(nameof(LocomotiveUsageCoverageSummary));
    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ISolutionSession.SelectedProject))
            RefreshProjectDiagnostics();
    }

    /// <summary>
    /// A persisted model change reported by the solution session, before it saves.
    /// The session already refreshes the runtime's project snapshot.
    /// </summary>
    private void OnSolutionModelChanged(object? sender, PropertyChangedEventArgs e) => RefreshProjectDiagnostics();

    partial void OnIsDiagnosticsExpandedChanged(bool value)
    {
        if (value)
            RefreshProjectDiagnostics();
    }

    partial void OnShowDiagnosticErrorsChanged(bool value)
    {
        _ = value;
        ApplyDiagnosticFilter();
    }

    partial void OnShowDiagnosticWarningsChanged(bool value)
    {
        _ = value;
        ApplyDiagnosticFilter();
    }

    partial void OnShowDiagnosticInformationChanged(bool value)
    {
        _ = value;
        ApplyDiagnosticFilter();
    }

    [RelayCommand]
    private void RefreshProjectDiagnostics()
    {
        var selectedProject = _session.SelectedProject?.Model;
        ProjectDiagnostics.Clear();
        foreach (var diagnostic in _projectDiagnosticsService.Analyze(selectedProject))
            ProjectDiagnostics.Add(diagnostic);

        // The Z21 assignment depends on the other projects of the solution.
        foreach (var diagnostic in Z21AssignmentDiagnostics.Analyze(selectedProject, _session.Solution.Projects))
            ProjectDiagnostics.Add(diagnostic);

        var currentFindingIds = ProjectDiagnostics.Select(diagnostic => diagnostic.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var finding in _usage.Diagnostics)
        {
            if (!currentFindingIds.Add(finding.FindingId))
                continue;
            ProjectDiagnostics.Add(ProjectDiagnosticsService.FromLocomotiveTrainReference(finding,
                finding.Location.Source == LocomotiveTrainSource.LoadedRuntimeDefinition
                    ? _usage.RuntimeObservation?.ObservedAtUtc : null));
        }

        OnPropertyChanged(nameof(DiagnosticErrorCount));
        OnPropertyChanged(nameof(DiagnosticWarningCount));
        OnPropertyChanged(nameof(DiagnosticInformationCount));
        OnPropertyChanged(nameof(DiagnosticsSummary));
        ApplyDiagnosticFilter();
    }

    private void ApplyDiagnosticFilter()
    {
        VisibleProjectDiagnostics.Clear();
        foreach (var diagnostic in ProjectDiagnostics.Where(diagnostic =>
                     IsDiagnosticVisible(diagnostic, ShowDiagnosticErrors, ShowDiagnosticWarnings, ShowDiagnosticInformation)))
            VisibleProjectDiagnostics.Add(diagnostic);
    }

    private static bool IsDiagnosticVisible(ProjectDiagnostic diagnostic, bool showErrors, bool showWarnings, bool showInformation)
    {
        if (diagnostic.Coverage?.State == LocomotiveTrainCoverageState.Unknown)
            return true;
        return diagnostic.Severity switch
        {
            ProjectDiagnosticSeverity.Error => showErrors,
            ProjectDiagnosticSeverity.Warning => showWarnings,
            ProjectDiagnosticSeverity.Information => showInformation,
            _ => true
        };
    }

    /// <summary>Detaches the usage and session subscriptions; the pane keeps its last findings.</summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _usage.PropertyChanged -= OnLocomotiveUsageChanged;
        _session.PropertyChanged -= OnSessionPropertyChanged;
        _session.ModelChanged -= OnSolutionModelChanged;
    }
}
