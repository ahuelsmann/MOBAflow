// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Backend.Service;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Domain.Enum;
using Interface;
using System.Collections.ObjectModel;

public enum MaintenanceFleetFilter
{
    All,
    DueSoon,
    Due,
    Overdue
}

public sealed record MaintenanceFleetFilterOption(MaintenanceFleetFilter Value, string Label);

public sealed record MaintenanceTaskTemplateOption(string Label, MaintenanceCategory Category);

public sealed record VehicleMaintenanceHistoryItemViewModel(
    DateTimeOffset RecordedAt,
    string Title,
    string Details);

/// <summary>
/// Shared presentation model for calendar-based maintenance on all rolling-stock pages.
/// </summary>
public sealed partial class RollingStockMaintenanceViewModel : ObservableObject
{
    private const double WholeNumberTolerance = 1e-9;

    private static readonly MaintenanceDueSoonThresholds DefaultDueSoonThresholds = new(TimeSpan.FromDays(30));

    private readonly IVehicleMaintenanceService _maintenanceService;
    private readonly IProjectContext? _projectContext;
    private readonly TimeProvider _timeProvider;
    private ProjectViewModel? _project;
    private object? _selectedVehicle;
    private TrainVehicleKind _vehicleKind;
    private string _searchText = string.Empty;
    private bool _isRefreshingFleet;

    public RollingStockMaintenanceViewModel(
        IVehicleMaintenanceService maintenanceService,
        IProjectContext? projectContext = null,
        TimeProvider? timeProvider = null)
    {
        _maintenanceService = maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));
        _projectContext = projectContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _selectedFilter = FilterOptions[0];
        _selectedTaskTemplate = TaskTemplates[0];
    }

    public IReadOnlyList<MaintenanceFleetFilterOption> FilterOptions { get; } =
    [
        new(MaintenanceFleetFilter.All, "All vehicles"),
        new(MaintenanceFleetFilter.DueSoon, "Due soon"),
        new(MaintenanceFleetFilter.Due, "Due now"),
        new(MaintenanceFleetFilter.Overdue, "Overdue")
    ];

    public IReadOnlyList<MaintenanceTaskTemplateOption> TaskTemplates { get; } =
    [
        new("Custom task", MaintenanceCategory.Inspection),
        new("Lubrication", MaintenanceCategory.Lubrication),
        new("Wheel cleaning", MaintenanceCategory.WheelService),
        new("Traction-tire inspection", MaintenanceCategory.WheelService),
        new("Coupler inspection", MaintenanceCategory.Inspection),
        new("Electrical pickup cleaning", MaintenanceCategory.Cleaning)
    ];

    public ObservableCollection<LocomotiveViewModel> VisibleLocomotives { get; } = [];

    public ObservableCollection<PassengerWagonViewModel> VisiblePassengerWagons { get; } = [];

    public ObservableCollection<GoodsWagonViewModel> VisibleGoodsWagons { get; } = [];

    public ObservableCollection<MaintenancePlanStatus> MaintenancePlans { get; } = [];

    public ObservableCollection<VehicleMaintenanceHistoryItemViewModel> History { get; } = [];

    [ObservableProperty]
    private MaintenanceFleetFilterOption _selectedFilter;

    [ObservableProperty]
    private MaintenanceTaskTemplateOption _selectedTaskTemplate;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompleteSelectedPlanCommand))]
    private MaintenancePlanStatus? _selectedPlan;

    [ObservableProperty]
    private string _vehicleName = string.Empty;

    [ObservableProperty]
    private string _nextTaskName = "No maintenance task scheduled";

    [ObservableProperty]
    private string _nextTaskStatusText = string.Empty;

    [ObservableProperty]
    private bool _hasSelectedVehicle;

    [ObservableProperty]
    private bool _hasMaintenancePlans;

    [ObservableProperty]
    private bool _hasHistory;

    [ObservableProperty]
    private string _operationStatus = string.Empty;

    [ObservableProperty]
    private string _newPlanName = "New maintenance task";

    [ObservableProperty]
    private double _newPlanIntervalDays = 30;

    public void SetContext(
        ProjectViewModel? project,
        TrainVehicleKind vehicleKind,
        object? selectedVehicle,
        string? searchText = null)
    {
        _project = project;
        _vehicleKind = vehicleKind;
        _selectedVehicle = selectedVehicle switch
        {
            LocomotiveViewModel locomotive => locomotive.Model,
            WagonViewModel wagon => wagon.Model,
            Locomotive or Wagon => selectedVehicle,
            _ => null
        };
        _searchText = searchText?.Trim() ?? string.Empty;

        RefreshFleet();
        RefreshSelectedVehicle();
    }

    partial void OnSelectedFilterChanged(MaintenanceFleetFilterOption value)
    {
        _ = value;
        RefreshFleet();
    }

    partial void OnSelectedTaskTemplateChanged(MaintenanceTaskTemplateOption value)
    {
        if (value.Label != "Custom task")
            NewPlanName = value.Label;
    }

    [RelayCommand(CanExecute = nameof(CanEditSelectedVehicle))]
    private async Task AddMaintenancePlanAsync()
    {
        if (_selectedVehicle is null)
            return;

        if (!TryCreateMaintenancePlan(out var plan, out var validationMessage))
        {
            OperationStatus = validationMessage;
            return;
        }

        EnsureMaintenance(_selectedVehicle).Plans.Add(plan!);
        await SaveChangesAsync();
        OperationStatus = "Maintenance task added.";
        Refresh();
    }

    [RelayCommand(CanExecute = nameof(CanCompleteSelectedPlan))]
    private async Task CompleteSelectedPlanAsync()
    {
        if (_selectedVehicle is null || SelectedPlan is null)
            return;

        var maintenance = GetMaintenance(_selectedVehicle);
        if (maintenance is null)
            return;

        try
        {
            _maintenanceService.CompletePlan(
                maintenance,
                SelectedPlan.PlanId,
                _timeProvider.GetUtcNow());
            await SaveChangesAsync();
            OperationStatus = "Maintenance recorded and the next due date was updated.";
            Refresh();
        }
        catch (ArgumentException exception)
        {
            OperationStatus = exception.Message;
        }
    }

    private bool CanEditSelectedVehicle() => _selectedVehicle is not null;

    private bool CanCompleteSelectedPlan() => _selectedVehicle is not null && SelectedPlan is not null;

    private void Refresh()
    {
        RefreshFleet();
        RefreshSelectedVehicle();
    }

    private void RefreshFleet()
    {
        // List selection can call SetContext again while a collection notification is in progress.
        if (_isRefreshingFleet)
            return;

        _isRefreshingFleet = true;
        try
        {
            UpdateVisibleFleet();
        }
        finally
        {
            _isRefreshingFleet = false;
        }
    }

    private void UpdateVisibleFleet()
    {
        var project = _project;
        if (project is null)
        {
            VisibleLocomotives.Clear();
            VisiblePassengerWagons.Clear();
            VisibleGoodsWagons.Clear();
            return;
        }

        ReplaceItems(
            VisibleLocomotives,
            _vehicleKind == TrainVehicleKind.Locomotive
                ? project.Locomotives.Where(item => MatchesFleetFilter(item.Model, item.Name))
                : []);
        ReplaceItems(
            VisiblePassengerWagons,
            _vehicleKind == TrainVehicleKind.PassengerWagon
                ? project.PassengerWagons.Where(item => MatchesFleetFilter(item.Model, item.Name))
                : []);
        ReplaceItems(
            VisibleGoodsWagons,
            _vehicleKind == TrainVehicleKind.GoodsWagon
                ? project.GoodsWagons.Where(item => MatchesFleetFilter(item.Model, item.Name))
                : []);
    }

    private bool MatchesFleetFilter(object vehicle, string name)
    {
        if (_searchText.Length != 0 && !name.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            return false;

        if (SelectedFilter.Value == MaintenanceFleetFilter.All)
            return true;

        return EvaluateHighestState(vehicle) == SelectedFilter.Value switch
        {
            MaintenanceFleetFilter.DueSoon => MaintenanceDueState.DueSoon,
            MaintenanceFleetFilter.Due => MaintenanceDueState.Due,
            MaintenanceFleetFilter.Overdue => MaintenanceDueState.Overdue,
            _ => MaintenanceDueState.NotScheduled
        };
    }

    private MaintenanceDueState EvaluateHighestState(object vehicle)
    {
        var maintenance = GetMaintenance(vehicle);
        if (maintenance is null || maintenance.Plans.Count == 0)
            return MaintenanceDueState.NotScheduled;

        try
        {
            return _maintenanceService.Evaluate(
                    maintenance,
                    _timeProvider.GetUtcNow(),
                    DefaultDueSoonThresholds)
                .Select(status => status.State)
                .DefaultIfEmpty(MaintenanceDueState.NotScheduled)
                .Max();
        }
        catch (ArgumentException)
        {
            return MaintenanceDueState.NotScheduled;
        }
    }

    private void RefreshSelectedVehicle()
    {
        MaintenancePlans.Clear();
        History.Clear();
        SelectedPlan = null;
        HasSelectedVehicle = _selectedVehicle is not null;
        AddMaintenancePlanCommand.NotifyCanExecuteChanged();
        CompleteSelectedPlanCommand.NotifyCanExecuteChanged();

        if (_selectedVehicle is null)
        {
            VehicleName = string.Empty;
            NextTaskName = "No vehicle selected";
            NextTaskStatusText = "Select a vehicle to view maintenance.";
            HasMaintenancePlans = false;
            HasHistory = false;
            return;
        }

        VehicleName = GetName(_selectedVehicle);
        RefreshPlans(_selectedVehicle);
        RefreshHistory(_selectedVehicle);
    }

    private void RefreshPlans(object vehicle)
    {
        var maintenance = GetMaintenance(vehicle);
        if (maintenance is not null)
        {
            try
            {
                foreach (var status in _maintenanceService.Evaluate(
                             maintenance,
                             _timeProvider.GetUtcNow(),
                             DefaultDueSoonThresholds))
                {
                    MaintenancePlans.Add(status);
                }
            }
            catch (ArgumentException exception)
            {
                OperationStatus = exception.Message;
            }
        }

        HasMaintenancePlans = MaintenancePlans.Count != 0;
        var next = MaintenancePlans.FirstOrDefault();
        NextTaskName = next?.Name ?? "No maintenance task scheduled";
        NextTaskStatusText = next is null ? "Add a task to start maintenance planning." : FormatPlanStatus(next);
    }

    private void RefreshHistory(object vehicle)
    {
        var maintenanceHistory = GetMaintenance(vehicle)?.Entries ?? [];
        var items = maintenanceHistory
            .Select(entry => new VehicleMaintenanceHistoryItemViewModel(
                entry.PerformedAt,
                entry.Description,
                "Maintenance completed"))
            .OrderByDescending(item => item.RecordedAt)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
            History.Add(item);
        HasHistory = History.Count != 0;
    }

    private bool TryCreateMaintenancePlan(out VehicleMaintenancePlan? plan, out string validationMessage)
    {
        plan = null;
        if (string.IsNullOrWhiteSpace(NewPlanName))
        {
            validationMessage = "Enter a maintenance task name.";
            return false;
        }

        if (!TryPositiveWholeNumber(NewPlanIntervalDays, out var intervalDays)
            || intervalDays == 0
            || intervalDays > int.MaxValue)
        {
            validationMessage = "Enter a positive whole number of days.";
            return false;
        }

        plan = new VehicleMaintenancePlan
        {
            Name = NewPlanName.Trim(),
            Category = SelectedTaskTemplate.Category,
            IntervalDays = checked((int)intervalDays),
            LastCompletedAt = _timeProvider.GetUtcNow()
        };
        validationMessage = string.Empty;
        return true;
    }

    private static VehicleMaintenanceData? GetMaintenance(object vehicle) => vehicle switch
    {
        Locomotive locomotive => locomotive.Maintenance,
        Wagon wagon => wagon.Maintenance,
        _ => null
    };

    private static VehicleMaintenanceData EnsureMaintenance(object vehicle)
    {
        return vehicle switch
        {
            Locomotive locomotive => locomotive.Maintenance ??= new VehicleMaintenanceData(),
            Wagon wagon => wagon.Maintenance ??= new VehicleMaintenanceData(),
            _ => throw new ArgumentException("Unsupported rolling-stock vehicle.", nameof(vehicle))
        };
    }

    private static string GetName(object vehicle) => vehicle switch
    {
        Locomotive locomotive => locomotive.Name,
        Wagon wagon => wagon.Name,
        _ => string.Empty
    };

    private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        var items = source.ToArray();
        if (target.SequenceEqual(items))
            return;

        target.Clear();
        foreach (var item in items)
            target.Add(item);
    }

    private static bool TryPositiveWholeNumber(double value, out long result)
    {
        if (!double.IsFinite(value)
            || value < 0
            || Math.Abs(value - Math.Round(value)) > WholeNumberTolerance
            || value > long.MaxValue)
        {
            result = 0;
            return false;
        }

        result = (long)value;
        return true;
    }

    private static string FormatPlanStatus(MaintenancePlanStatus status)
        => status.DueAt is { } dueAt ? $"{status.State}: due {dueAt:d}" : status.State.ToString();

    private Task SaveChangesAsync()
        => _projectContext?.SaveSolutionInternalAsync() ?? Task.CompletedTask;
}
