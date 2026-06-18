// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Common.Extension;
using Domain;
using Moba.SharedUI.ViewModel;
using System.ComponentModel;
using TrackLibrary.PikoA;

/// <summary>
/// Singleton bridge between the shared <see cref="EditableTrackPlan"/> (singleton in DI)
/// and the currently selected <see cref="Project"/> inside the <see cref="Solution"/>.
///
/// Responsibilities (hybrid persistence):
/// - On every plan mutation, mirror the editor state into <see cref="Project.TrackPlan"/>
///   so <c>solution.json</c> always contains the latest plan without needing the
///   <c>TrackPlanPage</c> to be open.
/// - On solution load / project switch, hydrate the editor plan from
///   <see cref="Project.TrackPlan"/>.
/// </summary>
public sealed class TrackPlanSolutionBinder
{
    private readonly EditableTrackPlan _plan;
    private readonly MainWindowViewModel _mainViewModel;
    private bool _suppressPlanChanged;

    public TrackPlanSolutionBinder(EditableTrackPlan plan, MainWindowViewModel mainViewModel)
    {
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
    }

    /// <summary>
    /// Activates the two-way binding. Call once during application startup
    /// (after MainWindowViewModel + EditableTrackPlan are available).
    /// </summary>
    public void Activate()
    {
        _plan.PlanChanged += OnPlanChanged;
        _mainViewModel.SolutionLoaded += OnSolutionLoaded;
        _mainViewModel.SolutionSaving += OnSolutionSaving;
        _mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;

        // Initial pull from whatever project is already selected at startup.
        LoadFromSelectedProject();
    }

    private void OnPlanChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        if (_suppressPlanChanged)
            return;

        PushPlanToSelectedProject();

        // Trigger the existing solution auto-save pipeline so additions/removals persist
        // before shutdown (EditableTrackPlan is not a ViewModel, so it does not feed
        // the PropertyChanged-based auto-save hook in MainWindowViewModel.SolutionAutoSave).
        _mainViewModel.SaveSolutionInternalAsync()
            .Observe(_ => { /* logging handled inside SaveSolutionInternalAsync */ });
    }

    private void OnSolutionSaving(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        // Ensure the latest editor state is flushed into project.TrackPlan
        // right before the Solution is serialized (manual save, auto-save, and shutdown-save).
        PushPlanToSelectedProject();
    }

    private void PushPlanToSelectedProject()
    {
        var project = _mainViewModel.SelectedProject?.Model;
        if (project == null)
            return;

        project.TrackPlan = TrackPlanEditorDocument
            .FromEditableTrackPlan(_plan)
            .ToDomainDocument();
    }

    private void OnSolutionLoaded(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        LoadFromSelectedProject();
    }

    private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedProject))
        {
            LoadFromSelectedProject();
        }
    }

    private void LoadFromSelectedProject()
    {
        var project = _mainViewModel.SelectedProject?.Model;
        if (project == null)
            return;

        _suppressPlanChanged = true;
        try
        {
            if (project.TrackPlan == null)
            {
                if (_plan.Segments.Count > 0 || _plan.Connections.Count > 0)
                {
                    _plan.LoadFromPlacements([], []);
                }
                return;
            }

            var (placements, connections) = TrackPlanEditorDocument
                .FromDomainDocument(project.TrackPlan)
                .ToEditableTrackPlanData();
            _plan.LoadFromPlacements(placements, connections);
        }
        finally
        {
            _suppressPlanChanged = false;
        }
    }
}