// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Domain.Enum;
using System.Collections.ObjectModel;
using System.Text.Json;

public sealed record EventElementDescriptor(string Id, string Name, string Description, uint DefaultRepeatCount);

/// <summary>Edits the feedback sequence of the journey selected in the shared application context.</summary>
public sealed partial class EventManagerViewModel : ObservableObject
{
    private readonly MainWindowViewModel _main;
    private readonly Stack<string> _undo = [];
    private readonly Stack<string> _redo = [];

    [ObservableProperty] private JourneyViewModel? _selectedJourney;
    [ObservableProperty] private JourneyFeedbackStepViewModel? _selectedStep;
    [ObservableProperty] private uint _defaultInPort = 1;
    [ObservableProperty] private string _workflowSearchText = string.Empty;
    [ObservableProperty] private string _stationSearchText = string.Empty;

    public EventManagerViewModel(MainWindowViewModel main)
    {
        _main = main;
        ToolboxElements =
        [
            new("single", "Single Feedback", "One matching feedback activation", 1),
            new("repeat", "Repeat Feedback", "Wait for multiple matching activations", 10)
        ];
        _main.PropertyChanged += OnMainPropertyChanged;
        SelectedJourney = _main.SelectedJourney;
        Refresh();
    }

    public ObservableCollection<JourneyFeedbackStepViewModel> Steps { get; } = [];
    public IReadOnlyList<EventElementDescriptor> ToolboxElements { get; }
    public IEnumerable<JourneyViewModel> Journeys => _main.SelectedProject?.Journeys ?? [];
    public WorkflowLibraryViewModel WorkflowLibrary => _main.WorkflowLibrary;
    public IEnumerable<WorkflowViewModel> Workflows => WorkflowLibrary.FilteredWorkflows;
    public IEnumerable<StationAssignmentOption> Stations
    {
        get
        {
            var options = new List<StationAssignmentOption> { new("Next stop", JourneyStopTransitionMode.Next) };
            options.AddRange((SelectedJourney?.Model.Stations ?? [])
                .Where(station => string.IsNullOrWhiteSpace(StationSearchText) || station.Name.Contains(StationSearchText, StringComparison.OrdinalIgnoreCase))
                .Select(station => new StationAssignmentOption(station.Name, JourneyStopTransitionMode.SpecificStation, station)));
            return options;
        }
    }
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    partial void OnSelectedJourneyChanged(JourneyViewModel? oldValue, JourneyViewModel? newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= OnSelectedJourneyPropertyChanged;
        if (_main.SelectedJourney != newValue) _main.SelectedJourney = newValue;
        if (newValue != null) newValue.PropertyChanged += OnSelectedJourneyPropertyChanged;
        _undo.Clear();
        _redo.Clear();
        Refresh();
        OnPropertyChanged(nameof(Stations));
    }

    partial void OnSelectedStepChanged(JourneyFeedbackStepViewModel? value)
    {
        _ = value;
        WorkflowLibrary.SelectedStep = null;
    }

    partial void OnWorkflowSearchTextChanged(string value)
    {
        WorkflowLibrary.SearchText = value;
        OnPropertyChanged(nameof(Workflows));
    }
    partial void OnStationSearchTextChanged(string value) { _ = value; OnPropertyChanged(nameof(Stations)); }

    [RelayCommand]
    public void AddElement(EventElementDescriptor descriptor) => InsertElement(descriptor, SelectedStep == null ? Steps.Count : Steps.IndexOf(SelectedStep) + 1);

    public void InsertElement(EventElementDescriptor descriptor, int index)
    {
        if (SelectedJourney == null) return;
        CaptureUndo();
        var step = new JourneyFeedbackStep { InPort = Math.Clamp(DefaultInPort, 1u, 512u), RepeatCount = descriptor.DefaultRepeatCount };
        index = Math.Clamp(index, 0, SelectedJourney.Model.FeedbackSequence.Count);
        SelectedJourney.Model.FeedbackSequence.Insert(index, step);
        Refresh(step.Id);
    }

    [RelayCommand]
    private void DeleteStep(JourneyFeedbackStepViewModel? step)
    {
        if (SelectedJourney == null || step == null) return;
        CaptureUndo();
        SelectedJourney.Model.FeedbackSequence.Remove(step.Model);
        Refresh();
    }

    public void MoveStep(JourneyFeedbackStepViewModel step, int targetIndex)
    {
        if (SelectedJourney == null) return;
        var sourceIndex = SelectedJourney.Model.FeedbackSequence.IndexOf(step.Model);
        targetIndex = Math.Clamp(targetIndex, 0, SelectedJourney.Model.FeedbackSequence.Count);
        if (sourceIndex < 0 || sourceIndex == targetIndex || sourceIndex + 1 == targetIndex) return;
        CaptureUndo();
        SelectedJourney.Model.FeedbackSequence.RemoveAt(sourceIndex);
        if (targetIndex > sourceIndex) targetIndex--;
        targetIndex = Math.Clamp(targetIndex, 0, SelectedJourney.Model.FeedbackSequence.Count);
        SelectedJourney.Model.FeedbackSequence.Insert(targetIndex, step.Model);
        Refresh(step.Model.Id);
    }

    [RelayCommand]
    private void Undo()
    {
        if (SelectedJourney == null || _undo.Count == 0) return;
        _redo.Push(SerializeSequence());
        RestoreSequence(_undo.Pop());
    }

    [RelayCommand]
    private void Redo()
    {
        if (SelectedJourney == null || _redo.Count == 0) return;
        _undo.Push(SerializeSequence());
        RestoreSequence(_redo.Pop());
    }

    public void CaptureUndo()
    {
        if (SelectedJourney == null) return;
        var snapshot = SerializeSequence();
        if (_undo.TryPeek(out var previous) && previous == snapshot) return;
        _undo.Push(snapshot);
        _redo.Clear();
        NotifyHistoryChanged();
    }

    private void Refresh(Guid? selectId = null)
    {
        Steps.Clear();
        if (SelectedJourney != null)
        {
            foreach (var step in SelectedJourney.Model.FeedbackSequence)
                Steps.Add(new JourneyFeedbackStepViewModel(step, _main.SelectedProject!.Model, SelectedJourney.Model, CaptureUndo));
        }
        SelectedStep = selectId.HasValue ? Steps.FirstOrDefault(step => step.Model.Id == selectId) : Steps.FirstOrDefault();
        UpdateRuntimeProgress();
        OnPropertyChanged(nameof(Journeys));
        OnPropertyChanged(nameof(Workflows));
        NotifyHistoryChanged();
    }

    private string SerializeSequence() => JsonSerializer.Serialize(SelectedJourney!.Model.FeedbackSequence, JsonOptions.Compact);
    private void RestoreSequence(string json)
    {
        SelectedJourney!.Model.FeedbackSequence = JsonSerializer.Deserialize<List<JourneyFeedbackStep>>(json, JsonOptions.Compact) ?? [];
        Refresh();
    }

    private void NotifyHistoryChanged()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private void OnMainPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedJourney)) SelectedJourney = _main.SelectedJourney;
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedProject)) Refresh();
    }

    private void OnSelectedJourneyPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(JourneyViewModel.CurrentFeedbackIndex) or nameof(JourneyViewModel.CurrentStepOccurrence))
            UpdateRuntimeProgress();
    }

    private void UpdateRuntimeProgress()
    {
        for (var index = 0; index < Steps.Count; index++)
            Steps[index].UpdateRuntimeProgress(index == SelectedJourney?.CurrentFeedbackIndex, SelectedJourney?.CurrentStepOccurrence ?? 0);
    }
}
