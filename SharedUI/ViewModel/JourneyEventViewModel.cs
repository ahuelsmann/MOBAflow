// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using System.Globalization;
using System.Runtime.CompilerServices;

/// <summary>Editable count condition; its count is independent of its display position.</summary>
public sealed partial class JourneyEventViewModel : ObservableObject
{
    private readonly Project _project;
    private readonly Func<bool> _canEdit;
    private readonly System.Action _beforeChange;
    private readonly System.Action _afterChange;
    private string _countText;

    public JourneyEventViewModel(JourneyEvent model, Project project, Func<bool> canEdit,
        System.Action beforeChange, System.Action afterChange)
    {
        Model = model;
        _project = project;
        _canEdit = canEdit;
        _beforeChange = beforeChange;
        _afterChange = afterChange;
        _countText = model.Count.ToString(CultureInfo.InvariantCulture);
    }

    public JourneyEvent Model { get; }
    public bool CanEdit => _canEdit();
    public uint InPort
    {
        get => Model.InPort;
        set => SetModelProperty(Model.InPort, Math.Clamp(value, 1u, 512u), (item, port) => item.InPort = port);
    }

    public ulong Count
    {
        get => Model.Count;
        set
        {
            if (!SetModelProperty(Model.Count, Math.Max(value, 1UL), (item, count) => item.Count = count)) return;
            _countText = Model.Count.ToString(CultureInfo.InvariantCulture);
            OnPropertyChanged(nameof(CountText));
            OnPropertyChanged(nameof(CountValidationMessage));
            OnPropertyChanged(nameof(HasCountValidationError));
        }
    }

    public string CountText
    {
        get => _countText;
        set
        {
            if (!CanEdit || !SetProperty(ref _countText, value)) return;
            if (ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var count) && count > 0) Count = count;
            OnPropertyChanged(nameof(CountValidationMessage));
            OnPropertyChanged(nameof(HasCountValidationError));
        }
    }

    public string CountValidationMessage =>
        ulong.TryParse(_countText, NumberStyles.None, CultureInfo.InvariantCulture, out var count) && count > 0
            ? string.Empty : "Enter a valid whole count of 1 or more.";

    public bool HasCountValidationError => CountValidationMessage.Length != 0;

    public Guid? WorkflowId
    {
        get => Model.WorkflowId;
        set
        {
            if (value.HasValue && _project.Workflows.All(workflow => workflow.Id != value)) return;
            if (SetModelProperty(Model.WorkflowId, value, (item, workflowId) => item.WorkflowId = workflowId)) RefreshWorkflows();
        }
    }

    public bool Enabled
    {
        get => Model.Enabled;
        set => SetModelProperty(Model.Enabled, value, (item, enabled) => item.Enabled = enabled);
    }

    public IEnumerable<Workflow> AvailableWorkflows => _project.Workflows;
    public string WorkflowName => WorkflowId.HasValue
        ? _project.Workflows.FirstOrDefault(workflow => workflow.Id == WorkflowId)?.Name ?? "Missing workflow"
        : "Choose a workflow";
    public string AutomationName => $"InPort {InPort}, count {Count} since journey start, workflow {WorkflowName}";

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void AssignWorkflow(WorkflowViewModel? workflow)
    {
        if (workflow != null) WorkflowId = workflow.Model.Id;
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void RemoveWorkflow() => WorkflowId = null;

    public void RefreshEditState()
    {
        OnPropertyChanged(nameof(CanEdit));
        AssignWorkflowCommand.NotifyCanExecuteChanged();
        RemoveWorkflowCommand.NotifyCanExecuteChanged();
    }

    public void RefreshWorkflows()
    {
        OnPropertyChanged(nameof(AvailableWorkflows));
        OnPropertyChanged(nameof(WorkflowName));
        OnPropertyChanged(nameof(AutomationName));
    }

    private bool SetModelProperty<T>(T oldValue, T newValue, System.Action<JourneyEvent, T> setter,
        [CallerMemberName] string? propertyName = null)
    {
        if (!CanEdit || EqualityComparer<T>.Default.Equals(oldValue, newValue)) return false;
        _beforeChange();
        SetProperty(oldValue, newValue, Model, setter, propertyName);
        OnPropertyChanged(nameof(AutomationName));
        _afterChange();
        return true;
    }
}
