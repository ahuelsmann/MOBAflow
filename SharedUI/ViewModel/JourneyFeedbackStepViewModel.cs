// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Domain.Enum;

/// <summary>Editable wrapper for one ordered journey feedback sequence entry.</summary>
public sealed partial class JourneyFeedbackStepViewModel : ObservableObject
{
    private readonly Project _project;
    private readonly Journey _journey;
    private readonly System.Action? _beforeChange;
    private bool _isCurrentRuntimeStep;
    private uint _runtimeOccurrence;

    public JourneyFeedbackStepViewModel(JourneyFeedbackStep model, Project project, Journey? journey = null, System.Action? beforeChange = null)
    {
        Model = model;
        _project = project;
        _journey = journey ?? new Journey();
        _beforeChange = beforeChange;
    }

    public JourneyFeedbackStep Model { get; }

    public uint InPort
    {
        get => Model.InPort;
        set
        {
            if (SetModelProperty(Model.InPort, value, (step, port) => step.InPort = port))
            {
                OnPropertyChanged(nameof(InPortText));
                OnPropertyChanged(nameof(AutomationName));
            }
        }
    }

    public Guid? WorkflowId
    {
        get => Model.WorkflowId;
        set
        {
            if (SetModelProperty(Model.WorkflowId, value, (step, workflowId) => step.WorkflowId = workflowId))
            {
                OnPropertyChanged(nameof(WorkflowName));
                OnPropertyChanged(nameof(HasWorkflow));
            }
        }
    }

    public uint RepeatCount
    {
        get => Model.RepeatCount;
        set
        {
            if (SetModelProperty(Model.RepeatCount, Math.Max(value, 1), (step, count) => step.RepeatCount = count))
            {
                OnPropertyChanged(nameof(IsRepeat));
                OnPropertyChanged(nameof(AutomationName));
            }
        }
    }

    public int DelayMs
    {
        get => Model.DelayMs;
        set => SetModelProperty(Model.DelayMs, Math.Max(value, 0), (step, delay) => step.DelayMs = delay);
    }

    public bool Enabled
    {
        get => Model.Enabled;
        set => SetModelProperty(Model.Enabled, value, (step, enabled) => step.Enabled = enabled);
    }

    public JourneyStopTransitionMode StopMode
    {
        get => Model.StopTransition.Mode;
        set
        {
            _beforeChange?.Invoke();
            if (!SetProperty(Model.StopTransition.Mode, value, Model.StopTransition, (transition, mode) => transition.Mode = mode)) return;
            if (value != JourneyStopTransitionMode.SpecificStation) Model.StopTransition.StationId = null;
            NotifyStopChanged();
        }
    }

    public Guid? TargetStationId
    {
        get => Model.StopTransition.StationId;
        set
        {
            _beforeChange?.Invoke();
            Model.StopTransition.Mode = value.HasValue ? JourneyStopTransitionMode.SpecificStation : JourneyStopTransitionMode.None;
            Model.StopTransition.StationId = value;
            NotifyStopChanged();
        }
    }

    public Guid? ConditionStationId
    {
        get => Model.Conditions.FirstOrDefault(condition => condition.Type == JourneyFeedbackConditionType.CurrentStationIs)?.StationId;
        set
        {
            if (ConditionStationId == value) return;
            _beforeChange?.Invoke();
            Model.Conditions.RemoveAll(condition => condition.Type == JourneyFeedbackConditionType.CurrentStationIs);
            if (value.HasValue)
                Model.Conditions.Add(new JourneyFeedbackCondition { Type = JourneyFeedbackConditionType.CurrentStationIs, StationId = value });
            OnPropertyChanged();
        }
    }

    public IEnumerable<Workflow> AvailableWorkflows => _project.Workflows;
    public IEnumerable<Station> AvailableStations => _journey.Stations;
    public IEnumerable<JourneyStopTransitionMode> StopModes => Enum.GetValues<JourneyStopTransitionMode>();
    public bool IsRepeat => RepeatCount > 1;
    public string InPortText => $"← InPort {InPort}";
    public bool HasWorkflow => WorkflowId.HasValue;
    public string WorkflowName => _project.Workflows.FirstOrDefault(workflow => workflow.Id == WorkflowId)?.Name ?? "No workflow";
    public string StopName => StopMode switch
    {
        JourneyStopTransitionMode.Next => "Next stop",
        JourneyStopTransitionMode.SpecificStation => _journey.Stations.FirstOrDefault(station => station.Id == TargetStationId)?.Name ?? "Missing stop",
        _ => "No stop change"
    };
    public string AutomationName => $"Feedback step, InPort {InPort}, repeat {RepeatCount}, workflow {WorkflowName}, stop {StopName}";
    public bool IsCurrentRuntimeStep
    {
        get => _isCurrentRuntimeStep;
        private set => SetProperty(ref _isCurrentRuntimeStep, value);
    }
    public string RuntimeProgress => IsCurrentRuntimeStep ? $"Current progress: {_runtimeOccurrence}/{RepeatCount}" : string.Empty;

    public void UpdateRuntimeProgress(bool isCurrent, uint occurrence)
    {
        IsCurrentRuntimeStep = isCurrent;
        _runtimeOccurrence = occurrence;
        OnPropertyChanged(nameof(RuntimeProgress));
        OnPropertyChanged(nameof(AutomationName));
    }

    [RelayCommand]
    private void AssignWorkflow(WorkflowViewModel? workflow)
    {
        if (workflow is null) return;

        WorkflowId = workflow.Model.Id;
    }

    [RelayCommand]
    private void RemoveWorkflow() => WorkflowId = null;

    [RelayCommand]
    private void AssignStation(StationAssignmentOption? option)
    {
        if (option is null) return;

        _beforeChange?.Invoke();
        Model.StopTransition.Mode = option.Mode;
        Model.StopTransition.StationId = option.Station?.Id;
        NotifyStopChanged();
    }

    [RelayCommand]
    private void RemoveStopTransition()
    {
        _beforeChange?.Invoke();
        Model.StopTransition.Mode = JourneyStopTransitionMode.None;
        Model.StopTransition.StationId = null;
        NotifyStopChanged();
    }

    private bool SetModelProperty<T>(T oldValue, T newValue, Action<JourneyFeedbackStep, T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue)) return false;
        _beforeChange?.Invoke();
        return SetProperty(oldValue, newValue, Model, setter);
    }

    private void NotifyStopChanged()
    {
        OnPropertyChanged(nameof(StopMode));
        OnPropertyChanged(nameof(TargetStationId));
        OnPropertyChanged(nameof(StopName));
        OnPropertyChanged(nameof(AutomationName));
    }
}

public sealed record StationAssignmentOption(string Name, JourneyStopTransitionMode Mode, Station? Station = null);
