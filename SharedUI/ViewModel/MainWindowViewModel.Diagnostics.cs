// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Backend.Service.Validation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

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
