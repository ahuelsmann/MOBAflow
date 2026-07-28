// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Backend.Service.Validation;
using CommunityToolkit.Mvvm.ComponentModel;
using Domain;

/// <summary>
/// Validation and persistence state for the selected object's definition.
/// </summary>
public enum DefinitionSaveState
{
    NotSaved,
    Saving,
    Saved,
    ValidationError
}

public sealed partial class InterlockingControlViewModel
{
    private readonly object _definitionSaveSync = new();
    private Task _definitionSaveTail = Task.CompletedTask;
    private long _definitionSaveRequestedVersion;

    [ObservableProperty]
    public partial DefinitionSaveState DefinitionSaveState { get; private set; } =
        DefinitionSaveState.NotSaved;

    [ObservableProperty]
    public partial string DefinitionSaveStatusText { get; private set; } =
        "Not saved";

    partial void OnDraftNameChanged(string value)
    {
        _ = value;
        if (_draftRouteId != Guid.Empty)
            ValidateAndRequestDefinitionSave();
    }

    /// <summary>
    /// Waits until every accepted definition save requested so far has completed.
    /// </summary>
    public Task WhenDefinitionSaveIdleAsync()
    {
        lock (_definitionSaveSync)
            return _definitionSaveTail;
    }

    private void ValidateAndRequestDefinitionSave()
    {
        var project = CurrentProject;
        if (project == null || _draftRouteId == Guid.Empty)
            return;

        var route = CreateDraftRoute();
        var report = ValidateRouteCandidate(project, route);
        ValidationMessages = report.Findings
            .Select(finding => $"{finding.Code}: {finding.Message}")
            .ToArray();
        OnPropertyChanged(nameof(ValidationMessages));
        if (!report.IsValid)
        {
            DefinitionSaveState = DefinitionSaveState.ValidationError;
            DefinitionSaveStatusText = "Not saved - resolve validation errors";
            SetStatus(
                "route.draft.invalid",
                $"Route definition has {report.Findings.Count} validation finding(s).");
            return;
        }

        ReplaceAuthoritativeRoute(project, route);
        RefreshDefinitions();
        QueueDefinitionSave(project, route);
    }

    private InterlockingValidationReport ValidateRouteCandidate(Project project, RouteDefinition route)
    {
        var routes = project.Interlocking.Routes;
        var existingIndex = routes.FindIndex(candidate => candidate.Id == route.Id);
        RouteDefinition? existing = null;
        if (existingIndex >= 0)
        {
            existing = routes[existingIndex];
            routes[existingIndex] = route;
        }
        else
        {
            routes.Add(route);
        }

        try
        {
            return _validator.Validate(project);
        }
        finally
        {
            if (existingIndex >= 0)
                routes[existingIndex] = existing!;
            else
                routes.Remove(route);
        }
    }

    private static void ReplaceAuthoritativeRoute(Project project, RouteDefinition route)
    {
        var routes = project.Interlocking.Routes;
        var existingIndex = routes.FindIndex(candidate => candidate.Id == route.Id);
        if (existingIndex >= 0)
            routes[existingIndex] = route;
        else
            routes.Add(route);
    }

    private void QueueDefinitionSave(Project project, RouteDefinition route)
    {
        var version = Interlocked.Increment(ref _definitionSaveRequestedVersion);
        DefinitionSaveState = DefinitionSaveState.Saving;
        DefinitionSaveStatusText = "Saving";
        lock (_definitionSaveSync)
        {
            _definitionSaveTail = PersistDefinitionAfterAsync(
                _definitionSaveTail,
                project,
                route,
                version);
        }
    }

    private async Task PersistDefinitionAfterAsync(
        Task previousSave,
        Project project,
        RouteDefinition route,
        long version)
    {
        await previousSave.ConfigureAwait(true);
        try
        {
            await _projectContext.SaveSolutionInternalAsync().ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                   or InvalidOperationException or NotSupportedException)
        {
            LogRoutePersistenceFailure(_logger, route.Id, ex);
            if (IsLatestDefinitionSave(version))
            {
                DefinitionSaveState = DefinitionSaveState.NotSaved;
                DefinitionSaveStatusText = $"Not saved - {ex.Message}";
            }
            return;
        }

        try
        {
            await _runtime.ActivateAsync(project.Interlocking).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
        {
            LogRouteActivationFailure(_logger, route.Id, ex);
            if (IsLatestDefinitionSave(version))
            {
                DefinitionSaveState = DefinitionSaveState.NotSaved;
                DefinitionSaveStatusText = "Not saved - live interlocking reload failed";
            }
            return;
        }

        if (!IsLatestDefinitionSave(version))
            return;

        if (_projectContext is MainWindowViewModel mainWindow &&
            mainWindow.SolutionSaveState != SolutionSaveState.Saved)
        {
            DefinitionSaveState = DefinitionSaveState.NotSaved;
            DefinitionSaveStatusText = mainWindow.SolutionSaveStatusText;
            return;
        }

        DefinitionSaveState = DefinitionSaveState.Saved;
        DefinitionSaveStatusText = "Saved";
        SetStatus("route.draft.saved", $"Route definition '{route.Name}' saved.");
    }

    private bool IsLatestDefinitionSave(long version) =>
        version == Volatile.Read(ref _definitionSaveRequestedVersion);
}
