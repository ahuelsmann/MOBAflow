// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Backend.Interface;
using Backend.Service;
using Common.Events;
using Common.Runtime;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Domain.Enum;
using Interface;
using System.Collections.ObjectModel;
using System.Globalization;

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
/// Shared presentation model for usage and maintenance on all rolling-stock pages.
/// </summary>
public sealed partial class RollingStockMaintenanceViewModel : ObservableObject
{
    private const double WholeNumberTolerance = 1e-9;

    private static readonly MaintenanceDueSoonThresholds DefaultDueSoonThresholds = new(
        TimeSpan.FromDays(30),
        OperatingSeconds: 10 * 60 * 60,
        CompletedTrips: 10,
        DistanceKilometres: 10);

    private readonly IVehicleUsageService _usageService;
    private readonly IVehicleMaintenanceService _maintenanceService;
    private readonly IMobaRuntime _runtime;
    private readonly IEventBus _eventBus;
    private readonly IProjectContext? _projectContext;
    private readonly TimeProvider _timeProvider;
    private ProjectViewModel? _project;
    private object? _selectedVehicle;
    private TrainVehicleKind _vehicleKind;
    private string _searchText = string.Empty;
    private IReadOnlyDictionary<Guid, VehicleUsageRuntimeSnapshot> _runtimeUsage;
    private Guid? _runtimeSubscriptionId;
    private Guid? _checkpointSubscriptionId;

    public RollingStockMaintenanceViewModel(
        IVehicleUsageService usageService,
        IVehicleMaintenanceService maintenanceService,
        IMobaRuntime runtime,
        IEventBus eventBus,
        IProjectContext? projectContext = null,
        TimeProvider? timeProvider = null)
    {
        _usageService = usageService ?? throw new ArgumentNullException(nameof(usageService));
        _maintenanceService = maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _projectContext = projectContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _runtimeUsage = runtime.Current.VehicleUsage;
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
    private string _operatingTimeText = "0 min";

    [ObservableProperty]
    private string _completedTripsText = "0";

    [ObservableProperty]
    private string _distanceText = "Not available";

    [ObservableProperty]
    private bool _hasDistance;

    [ObservableProperty]
    private bool _isOperating;

    [ObservableProperty]
    private string _operatingStatusText = "Not operating";

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
    private double _newPlanIntervalDays;

    [ObservableProperty]
    private double _newPlanOperatingHours;

    [ObservableProperty]
    private double _newPlanCompletedTrips;

    [ObservableProperty]
    private double _correctionOperatingHours;

    [ObservableProperty]
    private double _correctionCompletedTrips;

    [ObservableProperty]
    private string _correctionReason = string.Empty;

    public void Activate()
    {
        if (_runtimeSubscriptionId is not null)
            return;

        _runtimeSubscriptionId = _eventBus.Subscribe<RuntimeSnapshotChangedEvent>(OnRuntimeSnapshotChanged);
        _checkpointSubscriptionId = _eventBus.Subscribe<VehicleUsageCheckpointCommittedEvent>(OnUsageCheckpointCommitted);
        _runtimeUsage = _runtime.Current.VehicleUsage;
        Refresh();
    }

    public void Deactivate()
    {
        if (_runtimeSubscriptionId is { } runtimeSubscriptionId)
            _eventBus.Unsubscribe(runtimeSubscriptionId);
        if (_checkpointSubscriptionId is { } checkpointSubscriptionId)
            _eventBus.Unsubscribe(checkpointSubscriptionId);

        _runtimeSubscriptionId = null;
        _checkpointSubscriptionId = null;
    }

    public void SetContext(
        ProjectViewModel? project,
        TrainVehicleKind vehicleKind,
        object? selectedVehicle,
        string? searchText = null)
    {
        var normalizedSearchText = searchText?.Trim() ?? string.Empty;
        var fleetContextChanged =
            !ReferenceEquals(_project, project)
            || _vehicleKind != vehicleKind
            || !string.Equals(_searchText, normalizedSearchText, StringComparison.Ordinal);

        _project = project;
        _vehicleKind = vehicleKind;
        _selectedVehicle = selectedVehicle switch
        {
            LocomotiveViewModel locomotive => locomotive.Model,
            WagonViewModel wagon => wagon.Model,
            Locomotive or Wagon => selectedVehicle,
            _ => null
        };
        _searchText = normalizedSearchText;

        if (fleetContextChanged)
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
                BuildEffectiveUsage(_selectedVehicle),
                _timeProvider.GetUtcNow());
            await SaveChangesAsync();
            OperationStatus = "Maintenance recorded and the selected baseline was updated.";
            Refresh();
        }
        catch (ArgumentException exception)
        {
            OperationStatus = exception.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanEditSelectedVehicle))]
    private async Task RecordCorrectionAsync()
    {
        if (_selectedVehicle is null)
            return;

        if (!TryCreateCorrection(out var correction, out var validationMessage))
        {
            OperationStatus = validationMessage;
            return;
        }

        try
        {
            var usage = EnsureUsage(_selectedVehicle);
            SynchronizeTrackedUsageFromRuntime(_selectedVehicle, usage);
            _usageService.RecordCorrection(usage, correction!);
            await SaveChangesAsync();
            CorrectionOperatingHours = 0;
            CorrectionCompletedTrips = 0;
            CorrectionReason = string.Empty;
            OperationStatus = "Usage correction recorded in history.";
            Refresh();
        }
        catch (ArgumentException exception)
        {
            OperationStatus = exception.Message;
        }
    }

    private bool CanEditSelectedVehicle() => _selectedVehicle is not null;

    private bool CanCompleteSelectedPlan() => _selectedVehicle is not null && SelectedPlan is not null;

    private void OnRuntimeSnapshotChanged(RuntimeSnapshotChangedEvent runtimeEvent)
    {
        _runtimeUsage = runtimeEvent.Snapshot.VehicleUsage;
        Refresh();
    }

    private void OnUsageCheckpointCommitted(VehicleUsageCheckpointCommittedEvent checkpoint)
    {
        if (_project?.Model.Id != checkpoint.ProjectId)
            return;

        _runtimeUsage = checkpoint.Usage;
        Refresh();
    }

    private void Refresh()
    {
        RefreshFleet();
        RefreshSelectedVehicle();
    }

    private void RefreshFleet()
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
                    BuildEffectiveUsage(vehicle),
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
        RecordCorrectionCommand.NotifyCanExecuteChanged();
        CompleteSelectedPlanCommand.NotifyCanExecuteChanged();

        if (_selectedVehicle is null)
        {
            VehicleName = string.Empty;
            OperatingTimeText = "0 min";
            CompletedTripsText = "0";
            DistanceText = "Not available";
            HasDistance = false;
            IsOperating = false;
            OperatingStatusText = "Not operating";
            NextTaskName = "No vehicle selected";
            NextTaskStatusText = "Select a vehicle to view usage and maintenance.";
            HasMaintenancePlans = false;
            HasHistory = false;
            return;
        }

        VehicleName = GetName(_selectedVehicle);
        var usage = BuildEffectiveUsage(_selectedVehicle);
        var totals = _usageService.CalculateTotals(usage);
        OperatingTimeText = FormatDuration(totals.OperatingSeconds);
        CompletedTripsText = totals.CompletedTrips.ToString("N0", CultureInfo.CurrentCulture);
        HasDistance = totals.DistanceKilometres is not null;
        DistanceText = totals.DistanceKilometres is { } distance
            ? $"{distance:N2} km"
            : "Not available";

        var vehicleId = GetId(_selectedVehicle);
        IsOperating = _runtimeUsage.TryGetValue(vehicleId, out var runtimeUsage) && runtimeUsage.IsOperating;
        OperatingStatusText = IsOperating ? "Operating now" : "Not operating";

        RefreshPlans(_selectedVehicle, usage);
        RefreshHistory(_selectedVehicle);
    }

    private void RefreshPlans(object vehicle, VehicleUsageData usage)
    {
        var maintenance = GetMaintenance(vehicle);
        if (maintenance is not null)
        {
            try
            {
                foreach (var status in _maintenanceService.Evaluate(
                             maintenance,
                             usage,
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
        var usageHistory = GetUsage(vehicle)?.Corrections ?? [];
        var maintenanceHistory = GetMaintenance(vehicle)?.Entries ?? [];
        var items = usageHistory
            .Select(correction => new VehicleMaintenanceHistoryItemViewModel(
                correction.RecordedAt,
                "Usage correction",
                FormatCorrection(correction)))
            .Concat(maintenanceHistory.Select(entry => new VehicleMaintenanceHistoryItemViewModel(
                entry.PerformedAt,
                entry.Description,
                "Maintenance completed")))
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
            || !TryPositiveWholeNumber(NewPlanCompletedTrips, out var intervalTrips)
            || !TryOperatingSeconds(NewPlanOperatingHours, allowNegative: false, out var intervalSeconds)
            || intervalDays > int.MaxValue)
        {
            validationMessage = "Intervals must be positive whole days or trips and a valid number of hours.";
            return false;
        }
        if (intervalDays == 0 && intervalTrips == 0 && intervalSeconds == 0)
        {
            validationMessage = "Configure at least one maintenance interval.";
            return false;
        }

        var totals = _usageService.CalculateTotals(BuildEffectiveUsage(_selectedVehicle!));
        var now = _timeProvider.GetUtcNow();
        plan = new VehicleMaintenancePlan
        {
            Name = NewPlanName.Trim(),
            Category = SelectedTaskTemplate.Category,
            IntervalDays = intervalDays == 0 ? null : checked((int)intervalDays),
            IntervalOperatingSeconds = intervalSeconds == 0 ? null : intervalSeconds,
            IntervalCompletedTrips = intervalTrips == 0 ? null : intervalTrips,
            LastCompletedAt = intervalDays == 0 ? null : now,
            OperatingSecondsAtLastCompletion = intervalSeconds == 0 ? null : totals.OperatingSeconds,
            CompletedTripsAtLastCompletion = intervalTrips == 0 ? null : totals.CompletedTrips
        };
        validationMessage = string.Empty;
        return true;
    }

    private bool TryCreateCorrection(out VehicleUsageCorrection? correction, out string validationMessage)
    {
        correction = null;
        if (string.IsNullOrWhiteSpace(CorrectionReason))
        {
            validationMessage = "Enter a reason for the correction.";
            return false;
        }
        if (!TryOperatingSeconds(CorrectionOperatingHours, allowNegative: true, out var operatingSeconds)
            || !TrySignedWholeNumber(CorrectionCompletedTrips, out var completedTrips))
        {
            validationMessage = "Corrections must use a valid number of hours and whole trips.";
            return false;
        }
        if (operatingSeconds == 0 && completedTrips == 0)
        {
            validationMessage = "Adjust operating time or completed trips.";
            return false;
        }

        correction = new VehicleUsageCorrection
        {
            RecordedAt = _timeProvider.GetUtcNow(),
            OperatingSecondsDelta = operatingSeconds,
            CompletedTripsDelta = completedTrips,
            Reason = CorrectionReason.Trim()
        };
        validationMessage = string.Empty;
        return true;
    }

    private VehicleUsageData BuildEffectiveUsage(object vehicle)
    {
        var persisted = GetUsage(vehicle);
        var vehicleId = GetId(vehicle);
        _runtimeUsage.TryGetValue(vehicleId, out var runtime);
        return new VehicleUsageData
        {
            TrackedOperatingSeconds = runtime?.TrackedOperatingSeconds ?? persisted?.TrackedOperatingSeconds ?? 0,
            TrackedCompletedTrips = runtime?.TrackedCompletedTrips ?? persisted?.TrackedCompletedTrips ?? 0,
            TrackedDistanceKilometres = persisted?.TrackedDistanceKilometres,
            Corrections = persisted?.Corrections ?? []
        };
    }

    private void SynchronizeTrackedUsageFromRuntime(object vehicle, VehicleUsageData usage)
    {
        if (!_runtimeUsage.TryGetValue(GetId(vehicle), out var runtime))
            return;

        usage.TrackedOperatingSeconds = runtime.TrackedOperatingSeconds;
        usage.TrackedCompletedTrips = runtime.TrackedCompletedTrips;
    }

    private static VehicleUsageData? GetUsage(object vehicle) => vehicle switch
    {
        Locomotive locomotive => locomotive.Usage,
        Wagon wagon => wagon.Usage,
        _ => null
    };

    private static VehicleUsageData EnsureUsage(object vehicle)
    {
        return vehicle switch
        {
            Locomotive locomotive => locomotive.Usage ??= new VehicleUsageData(),
            Wagon wagon => wagon.Usage ??= new VehicleUsageData(),
            _ => throw new ArgumentException("Unsupported rolling-stock vehicle.", nameof(vehicle))
        };
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

    private static Guid GetId(object vehicle) => vehicle switch
    {
        Locomotive locomotive => locomotive.Id,
        Wagon wagon => wagon.Id,
        _ => Guid.Empty
    };

    private static string GetName(object vehicle) => vehicle switch
    {
        Locomotive locomotive => locomotive.Name,
        Wagon wagon => wagon.Name,
        _ => string.Empty
    };

    private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
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

    private static bool TrySignedWholeNumber(double value, out long result)
    {
        if (!double.IsFinite(value)
            || Math.Abs(value - Math.Round(value)) > WholeNumberTolerance
            || value < long.MinValue
            || value > long.MaxValue)
        {
            result = 0;
            return false;
        }

        result = (long)value;
        return true;
    }

    private static bool TryOperatingSeconds(double hours, bool allowNegative, out long seconds)
    {
        if (!double.IsFinite(hours) || !allowNegative && hours < 0)
        {
            seconds = 0;
            return false;
        }

        var value = hours * 60 * 60;
        if (value < long.MinValue || value > long.MaxValue)
        {
            seconds = 0;
            return false;
        }

        seconds = checked((long)Math.Round(value, MidpointRounding.AwayFromZero));
        return true;
    }

    private static string FormatDuration(long seconds)
    {
        var duration = TimeSpan.FromSeconds(seconds);
        var totalHours = (long)duration.TotalHours;
        return totalHours > 0
            ? $"{totalHours:N0} h {duration.Minutes:D2} min"
            : $"{duration.Minutes:N0} min";
    }

    private static string FormatPlanStatus(MaintenancePlanStatus status)
    {
        var remaining = new List<string>();
        if (status.DueAt is { } dueAt)
            remaining.Add($"due {dueAt:d}");
        if (status.RemainingOperatingSeconds is { } seconds)
            remaining.Add($"{FormatSignedDuration(seconds)} remaining");
        if (status.RemainingCompletedTrips is { } trips)
            remaining.Add($"{trips:N0} trips remaining");
        if (status.RemainingDistanceKilometres is { } distance)
            remaining.Add($"{distance:N2} km remaining");
        return remaining.Count == 0
            ? status.State.ToString()
            : $"{status.State}: {string.Join(", ", remaining)}";
    }

    private static string FormatCorrection(VehicleUsageCorrection correction)
    {
        var adjustments = new List<string>();
        if (correction.OperatingSecondsDelta != 0)
            adjustments.Add(FormatSignedDuration(correction.OperatingSecondsDelta));
        if (correction.CompletedTripsDelta != 0)
            adjustments.Add($"{correction.CompletedTripsDelta:+#;-#;0} trips");
        if (correction.DistanceKilometresDelta is { } distance)
            adjustments.Add($"{distance:+0.##;-0.##;0} km");
        return $"{string.Join(", ", adjustments)} - {correction.Reason}";
    }

    private static string FormatSignedDuration(long seconds)
    {
        var sign = string.Empty;
        if (seconds > 0)
            sign = "+";
        else if (seconds < 0)
            sign = "-";
        return $"{sign}{FormatDuration(Math.Abs(seconds))}";
    }

    private Task SaveChangesAsync()
        => _projectContext?.SaveSolutionInternalAsync() ?? Task.CompletedTask;
}
