// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Interface;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;

/// <summary>Edits the InPort count events of the selected journey.</summary>
public sealed partial class EventManagerViewModel : ObservableObject, IDisposable
{
    private readonly IProjectContext _context;
    private readonly Stack<string> _undo = [];
    private readonly Stack<string> _redo = [];
    private bool _notifyingJourney;
    private bool _disposed;

    [ObservableProperty] private JourneyViewModel? _selectedJourney;
    [ObservableProperty] private JourneyEventViewModel? _selectedEvent;
    [ObservableProperty] private uint _defaultInPort = 1;

    public EventManagerViewModel(MainWindowViewModel main)
        : this(main, main.WorkflowLibrary)
    {
        MainWindow = main;
        NotifyState();
    }

    public EventManagerViewModel(IProjectContext context, WorkflowLibraryViewModel workflowLibrary)
    {
        _context = context;
        WorkflowLibrary = workflowLibrary;
        _context.PropertyChanged += OnContextPropertyChanged;
        WorkflowLibrary.PropertyChanged += OnWorkflowLibraryPropertyChanged;
        SelectedJourney = _context.SelectedJourney;
        Refresh();
    }

    public MainWindowViewModel? MainWindow { get; }
    public WorkflowLibraryViewModel WorkflowLibrary { get; }
    public ObservableCollection<JourneyEventViewModel> Events { get; } = [];
    public IEnumerable<JourneyViewModel> Journeys => _context.SelectedProject?.Journeys ?? [];
    public string EventCountLabel => Events.Count == 1 ? "1 event" : $"{Events.Count} events";
    public bool IsEmptyPlan => SelectedJourney != null && Events.Count == 0;
    public bool HasCommandStatus => !string.IsNullOrWhiteSpace(MainWindow?.JourneyCommandStatus);
    public bool CanEdit => this.SelectedJourney != null;
    public bool CanUndo => CanEdit && _undo.Count > 0;
    public bool CanRedo => CanEdit && _redo.Count > 0;
    public bool CanEditSelectedEvent => CanEdit && SelectedEvent != null;

    public string PlanStatus => this.SelectedJourney switch
    {
        null => "Select a journey to edit its events.",
        { IsActive: false } => "Journey inactive. Activate it to evaluate its events on incoming feedback.",
        _ => "Each event runs its workflow when its InPort counter reaches the count. Counters start at zero after a reset."
    };

    partial void OnSelectedJourneyChanged(JourneyViewModel? oldValue, JourneyViewModel? newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= OnJourneyPropertyChanged;
        if (_context.SelectedJourney != newValue) _context.SelectedJourney = newValue;
        if (newValue != null) newValue.PropertyChanged += OnJourneyPropertyChanged;
        _undo.Clear();
        _redo.Clear();
        Refresh();
    }

    partial void OnSelectedEventChanged(JourneyEventViewModel? oldValue, JourneyEventViewModel? newValue)
    {
        if (oldValue != null) oldValue.IsSelected = false;
        if (newValue != null) newValue.IsSelected = true;
        NotifyCommands();
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void AddEvent() => InsertEvent(null, Events.Count);

    public void InsertEvent(WorkflowViewModel? workflow, int index)
    {
        var plan = SelectedJourney?.Model.EventPlan;
        if (plan == null || (workflow != null && !IsAvailableWorkflow(workflow))) return;
        CaptureUndo();
        var inPort = Math.Clamp(DefaultInPort, 1u, 512u);
        var previousCount = Events.Where(item => item.InPort == inPort).Select(item => item.Count).DefaultIfEmpty(0UL).Max();
        var item = new JourneyEvent
        {
            InPort = inPort,
            Count = previousCount == ulong.MaxValue ? previousCount : previousCount + 1,
            WorkflowId = workflow?.Model.Id
        };
        plan.Events.Insert(Math.Clamp(index, 0, plan.Events.Count), item);
        CompleteEdit(item.Id);
    }

    public bool IsAvailableWorkflow(WorkflowViewModel workflow) =>
        _context.SelectedProject?.Model.Workflows.Any(item => item.Id == workflow.Model.Id) == true;

    public bool ContainsEvent(JourneyEventViewModel item) => Events.Contains(item);

    [RelayCommand(CanExecute = nameof(CanEditSelectedEvent))]
    private void DuplicateSelectedEvent()
    {
        if (SelectedEvent != null) MoveOrCopyEvent(SelectedEvent, Events.IndexOf(SelectedEvent) + 1, true);
    }

    [RelayCommand(CanExecute = nameof(CanEditSelectedEvent))]
    private void DeleteSelectedEvent()
    {
        var plan = SelectedJourney?.Model.EventPlan;
        var selectedEvent = SelectedEvent;
        if (!CanEdit || plan == null || selectedEvent == null) return;
        CaptureUndo();
        plan.Events.Remove(selectedEvent.Model);
        CompleteEdit();
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveSelectedEventUp()
    {
        if (SelectedEvent != null) MoveOrCopyEvent(SelectedEvent, Events.IndexOf(SelectedEvent) - 1, false);
    }

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveSelectedEventDown()
    {
        if (SelectedEvent != null) MoveOrCopyEvent(SelectedEvent, Events.IndexOf(SelectedEvent) + 2, false);
    }

    private bool CanMoveUp() => CanEdit && SelectedEvent is { } item && Events.IndexOf(item) > 0;
    private bool CanMoveDown() => CanEdit && SelectedEvent is { } item && Events.IndexOf(item) < Events.Count - 1;

    public void MoveOrCopyEvent(JourneyEventViewModel item, int targetIndex, bool copy)
    {
        var plan = SelectedJourney?.Model.EventPlan;
        if (!CanEdit || plan == null || !ContainsEvent(item)) return;
        var events = plan.Events;
        var sourceIndex = events.IndexOf(item.Model);
        targetIndex = Math.Clamp(targetIndex, 0, events.Count);
        if (!copy && (sourceIndex == targetIndex || sourceIndex + 1 == targetIndex)) return;
        CaptureUndo();
        var model = item.Model;
        if (copy)
            model = new JourneyEvent { InPort = model.InPort, Count = model.Count, WorkflowId = model.WorkflowId, Enabled = model.Enabled };
        else
        {
            events.RemoveAt(sourceIndex);
            if (targetIndex > sourceIndex) targetIndex--;
        }
        events.Insert(targetIndex, model);
        CompleteEdit(model.Id);
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (!CanUndo) return;
        _redo.Push(SerializePlan());
        RestorePlan(_undo.Pop());
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (!CanRedo) return;
        _undo.Push(SerializePlan());
        RestorePlan(_redo.Pop());
    }

    private void CaptureUndo()
    {
        if (!CanEdit) return;
        var snapshot = SerializePlan();
        if (_undo.TryPeek(out var previous) && previous == snapshot) return;
        _undo.Push(snapshot);
        _redo.Clear();
        NotifyCommands();
    }

    private void CompleteEdit(Guid? selectId = null)
    {
        Refresh(selectId);
        NotifyJourneyChanged();
    }

    private void NotifyJourneyChanged()
    {
        _notifyingJourney = true;
        try { SelectedJourney?.NotifyEventPlanChanged(); }
        finally { _notifyingJourney = false; }
        NotifyState();
    }

    private void Refresh(Guid? selectId = null)
    {
        selectId ??= SelectedEvent?.Model.Id;
        Events.Clear();
        if (SelectedJourney != null && _context.SelectedProject != null)
        {
            foreach (var item in SelectedJourney.Model.EventPlan.Events)
                Events.Add(new JourneyEventViewModel(item, _context.SelectedProject.Model, () => CanEdit,
                    CaptureUndo, NotifyJourneyChanged));
        }
        SelectedEvent = Events.FirstOrDefault(item => item.Model.Id == selectId) ?? Events.FirstOrDefault();
        NotifyState();
    }

    private string SerializePlan() => JsonSerializer.Serialize(this.SelectedJourney?.Model.EventPlan, JsonOptions.Compact);

    private void RestorePlan(string json)
    {
        var journey = SelectedJourney;
        if (journey == null) return;
        journey.Model.EventPlan = JsonSerializer.Deserialize<JourneyEventPlan>(json, JsonOptions.Compact)
            ?? throw new InvalidOperationException("The saved event plan could not be restored.");
        CompleteEdit();
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(Journeys));
        OnPropertyChanged(nameof(EventCountLabel));
        OnPropertyChanged(nameof(IsEmptyPlan));
        OnPropertyChanged(nameof(HasCommandStatus));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(PlanStatus));
        foreach (var item in Events) item.RefreshEditState();
        NotifyCommands();
    }

    private void NotifyCommands()
    {
        OnPropertyChanged(nameof(CanEditSelectedEvent));
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        AddEventCommand.NotifyCanExecuteChanged();
        DeleteSelectedEventCommand.NotifyCanExecuteChanged();
        DuplicateSelectedEventCommand.NotifyCanExecuteChanged();
        MoveSelectedEventUpCommand.NotifyCanExecuteChanged();
        MoveSelectedEventDownCommand.NotifyCanExecuteChanged();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private void OnContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.JourneyCommandStatus)) OnPropertyChanged(nameof(HasCommandStatus));
        if (e.PropertyName == nameof(IProjectContext.SelectedJourney)) SelectedJourney = _context.SelectedJourney;
        if (e.PropertyName == nameof(IProjectContext.SelectedProject))
        {
            SelectedJourney = _context.SelectedJourney;
            Refresh();
        }
    }

    private void OnJourneyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(JourneyViewModel.EventPlan) && !_notifyingJourney)
        {
            _undo.Clear();
            _redo.Clear();
            Refresh();
        }
        else if (e.PropertyName == nameof(JourneyViewModel.IsActive)) OnPropertyChanged(nameof(PlanStatus));
    }

    private void OnWorkflowLibraryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        foreach (var item in Events) item.RefreshWorkflows();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _context.PropertyChanged -= OnContextPropertyChanged;
        WorkflowLibrary.PropertyChanged -= OnWorkflowLibraryPropertyChanged;
        if (SelectedJourney != null) SelectedJourney.PropertyChanged -= OnJourneyPropertyChanged;
        GC.SuppressFinalize(this);
    }
}
