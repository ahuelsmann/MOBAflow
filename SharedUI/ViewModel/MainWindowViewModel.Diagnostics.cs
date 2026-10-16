// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Backend.Service.Validation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

/// <summary>
/// Shell-level, state-based project diagnostics. This is intentionally separate from the continuous
/// application and Z21 logs shown on the Monitor page.
/// </summary>
public sealed partial class MainWindowViewModel
{
    private readonly IProjectDiagnosticsService? _projectDiagnosticsService;

    public ObservableCollection<ProjectDiagnostic> ProjectDiagnostics { get; } = [];

    public ObservableCollection<ProjectDiagnostic> VisibleProjectDiagnostics { get; } = [];

    [ObservableProperty]
    private bool _isDiagnosticsExpanded;

    [ObservableProperty]
    private bool _showDiagnosticErrors = true;

    [ObservableProperty]
    private bool _showDiagnosticWarnings = true;

    [ObservableProperty]
    private bool _showDiagnosticInformation = true;

    public int DiagnosticErrorCount =>
        ProjectDiagnostics.Count(item => item.Severity == ProjectDiagnosticSeverity.Error);

    public int DiagnosticWarningCount =>
        ProjectDiagnostics.Count(item => item.Severity == ProjectDiagnosticSeverity.Warning);

    public int DiagnosticInformationCount =>
        ProjectDiagnostics.Count(item => item.Severity == ProjectDiagnosticSeverity.Information);

    public string DiagnosticsSummary =>
        $"{DiagnosticErrorCount} errors, {DiagnosticWarningCount} warnings, {DiagnosticInformationCount} information";

    /// <summary>Source coverage remains visible even when neutral Information rows are filtered out.</summary>
    public string LocomotiveUsageCoverageSummary => LocomotivesPage.Usage.CoverageSummary;

    private void InitializeLocomotiveUsageDiagnostics() =>
        LocomotivesPage.Usage.PropertyChanged += OnLocomotiveUsageChanged;

    private void OnLocomotiveUsageChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(LocomotiveTrainUsageViewModel.Diagnostics))
        {
            if (LocomotivesPage.Usage.EditedResult is not null)
                IsDiagnosticsExpanded = true;
            RefreshProjectDiagnostics();
        }
        if (args.PropertyName == nameof(LocomotiveTrainUsageViewModel.CoverageSummary))
            OnPropertyChanged(nameof(LocomotiveUsageCoverageSummary));
    }

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
        ProjectDiagnostics.Clear();
        if (_projectDiagnosticsService is not null)
        {
            foreach (var diagnostic in _projectDiagnosticsService.Analyze(SelectedProject?.Model))
                ProjectDiagnostics.Add(diagnostic);
        }

        // The Z21 assignment depends on the other projects of the solution.
        foreach (var diagnostic in Z21AssignmentDiagnostics.Analyze(SelectedProject?.Model, Solution.Projects))
            ProjectDiagnostics.Add(diagnostic);

        var usage = LocomotivesPage.Usage;
        var currentFindingIds = ProjectDiagnostics.Select(diagnostic => diagnostic.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var finding in usage.Diagnostics)
        {
            if (!currentFindingIds.Add(finding.FindingId))
                continue;
            ProjectDiagnostics.Add(ProjectDiagnosticsService.FromLocomotiveTrainReference(finding,
                finding.Location.Source == Backend.Interface.LocomotiveTrainSource.LoadedRuntimeDefinition
                    ? usage.RuntimeObservation?.ObservedAtUtc : null));
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
        foreach (var diagnostic in ProjectDiagnostics.Where(IsDiagnosticVisible))
            VisibleProjectDiagnostics.Add(diagnostic);
    }

    private bool IsDiagnosticVisible(ProjectDiagnostic diagnostic) => diagnostic.Severity switch
    {
        ProjectDiagnosticSeverity.Error => ShowDiagnosticErrors,
        ProjectDiagnosticSeverity.Warning => ShowDiagnosticWarnings,
        ProjectDiagnosticSeverity.Information => ShowDiagnosticInformation,
        _ => true
    };
}
