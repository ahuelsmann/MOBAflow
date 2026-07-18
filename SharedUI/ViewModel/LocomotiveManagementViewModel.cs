// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Backend.Service;
using Backend.Service.Validation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Interface;
using System.Collections.ObjectModel;

public sealed record AddressFindingViewModel(
    string Id,
    string Message,
    DigitalAddressFindingKind Kind,
    IReadOnlyList<Guid> TargetIds);

/// <summary>
/// Presentation-only projection for locomotive quality, maintenance, decoder and passport data.
/// All business rules remain in backend services.
/// </summary>
public sealed partial class LocomotiveManagementViewModel : ObservableObject
{
    private readonly IDigitalAddressConflictDetector _conflictDetector;
    private readonly ILocomotiveMaintenanceService _maintenanceService;
    private readonly ILocomotiveLibraryService _libraryService;
    private readonly ILocomotivePassportHtmlRenderer? _passportRenderer;
    private readonly IDecoderCvService? _decoderCvService;
    private readonly IFilePickerService? _filePicker;
    private readonly IProjectContext? _projectContext;
    private Project? _project;
    private Locomotive? _locomotive;

    public LocomotiveManagementViewModel(
        IDigitalAddressConflictDetector conflictDetector,
        ILocomotiveMaintenanceService maintenanceService,
        ILocomotiveLibraryService libraryService,
        ILocomotivePassportHtmlRenderer? passportRenderer = null,
        IDecoderCvService? decoderCvService = null,
        IFilePickerService? filePicker = null,
        IProjectContext? projectContext = null)
    {
        _conflictDetector = conflictDetector ?? throw new ArgumentNullException(nameof(conflictDetector));
        _maintenanceService = maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));
        _libraryService = libraryService ?? throw new ArgumentNullException(nameof(libraryService));
        _passportRenderer = passportRenderer;
        _decoderCvService = decoderCvService;
        _filePicker = filePicker;
        _projectContext = projectContext;
    }

    public ObservableCollection<AddressFindingViewModel> AddressFindings { get; } = [];

    public ObservableCollection<MaintenancePlanStatus> MaintenancePlans { get; } = [];

    public ObservableCollection<DecoderCvSnapshot> DecoderSnapshots { get; } = [];

    public ObservableCollection<LocomotiveWhistleRule> WhistleRules { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAddressFindings))]
    [NotifyPropertyChangedFor(nameof(AddressFindingSummary))]
    private int _addressFindingCount;

    [ObservableProperty]
    private LocomotivePassport? _passport;

    [ObservableProperty]
    private string? _maintenanceValidationMessage;

    [ObservableProperty]
    private string? _operationStatus;

    [ObservableProperty]
    private LocomotiveWhistleRule? _selectedWhistleRule;

    public bool HasAddressFindings => AddressFindingCount > 0;

    public string AddressFindingSummary => $"{AddressFindingCount} finding(s) require attention.";

    public void SetContext(Project? project, Locomotive? locomotive, DateTimeOffset? now = null)
    {
        AddressFindings.Clear();
        MaintenancePlans.Clear();
        DecoderSnapshots.Clear();
        WhistleRules.Clear();
        SelectedWhistleRule = null;
        Passport = null;
        _project = project;
        _locomotive = locomotive;
        MaintenanceValidationMessage = null;

        if (project is not null)
        {
            foreach (var finding in _conflictDetector.Detect(project).Findings)
            {
                AddressFindings.Add(new AddressFindingViewModel(
                    finding.Id,
                    finding.Message,
                    finding.Kind,
                    finding.Owners.Select(owner => owner.Id).Distinct().ToArray()));
            }
        }
        AddressFindingCount = AddressFindings.Count;

        if (project is not null)
        {
            foreach (var rule in project.LocomotiveWhistleRules
                         .Where(rule => locomotive is null || rule.LocomotiveId == locomotive.Id)
                         .OrderBy(rule => rule.InPort)
                         .ThenBy(rule => rule.Id))
                WhistleRules.Add(rule);
        }

        if (locomotive is null)
            return;

        Passport = _libraryService.BuildPassport(locomotive);
        foreach (var snapshot in locomotive.Decoder?.CvSnapshots
                     .OrderByDescending(snapshot => snapshot.CapturedAt)
                     .ThenBy(snapshot => snapshot.Id) ?? Enumerable.Empty<DecoderCvSnapshot>())
            DecoderSnapshots.Add(snapshot);

        if (locomotive.Maintenance is not { } maintenance)
            return;

        var errors = _maintenanceService.Validate(maintenance);
        if (errors.Count != 0)
        {
            MaintenanceValidationMessage = string.Join(" ", errors);
            return;
        }

        foreach (var status in _maintenanceService.Evaluate(maintenance, now ?? DateTimeOffset.UtcNow))
            MaintenancePlans.Add(status);
    }

    [RelayCommand]
    private async Task AddMaintenancePlanAsync()
    {
        if (_locomotive is null)
            return;
        _locomotive.Maintenance ??= new LocomotiveMaintenanceData();
        _locomotive.Maintenance.Plans.Add(new LocomotiveMaintenancePlan
        {
            Name = "New maintenance reminder",
            Category = MaintenanceCategory.Inspection,
            LastCompletedAt = DateTimeOffset.UtcNow,
            IntervalDays = 365
        });
        SetContext(_project, _locomotive);
        await SaveChangesAsync();
        OperationStatus = "Maintenance reminder added.";
    }

    [RelayCommand]
    private async Task AddMaintenanceEntryAsync()
    {
        if (_locomotive is null)
            return;
        _locomotive.Maintenance ??= new LocomotiveMaintenanceData();
        _locomotive.Maintenance.Entries.Add(new LocomotiveMaintenanceEntry
        {
            Description = "Maintenance performed",
            Category = MaintenanceCategory.Inspection,
            PerformedAt = DateTimeOffset.UtcNow
        });
        SetContext(_project, _locomotive);
        await SaveChangesAsync();
        OperationStatus = "Maintenance entry added.";
    }

    [RelayCommand]
    private async Task AddWhistleRuleAsync()
    {
        if (_project is null || _locomotive is null)
            return;
        var rule = new LocomotiveWhistleRule
        {
            Name = "Station whistle",
            LocomotiveId = _locomotive.Id,
            InPort = 1,
            FunctionIndex = 2,
            ActiveDurationMilliseconds = 1000,
            Enabled = false
        };
        _project.LocomotiveWhistleRules.Add(rule);
        WhistleRules.Add(rule);
        SelectedWhistleRule = rule;
        await SaveChangesAsync();
        OperationStatus = "Whistle rule added. Configure its feedback input and timing.";
    }

    [RelayCommand]
    private async Task DeleteSelectedWhistleRuleAsync()
    {
        if (_project is null || SelectedWhistleRule is null)
            return;
        _project.LocomotiveWhistleRules.Remove(SelectedWhistleRule);
        WhistleRules.Remove(SelectedWhistleRule);
        SelectedWhistleRule = null;
        await SaveChangesAsync();
        OperationStatus = "Whistle rule removed.";
    }

    [RelayCommand]
    private async Task ExportPassportAsync()
    {
        if (Passport is null || _passportRenderer is null || _filePicker is null)
            return;
        try
        {
            var path = await _filePicker.SaveHtmlFileAsync(SafeFileName(Passport.Name) + "-passport");
            if (path is null)
                return;
            await File.WriteAllTextAsync(path, _passportRenderer.Render(Passport));
            OperationStatus = "Locomotive passport exported.";
        }
        catch (Exception exception)
        {
            OperationStatus = $"Passport export failed: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportLatestCvSnapshotAsync()
    {
        var snapshot = DecoderSnapshots.FirstOrDefault();
        if (snapshot is null || _decoderCvService is null || _filePicker is null)
            return;
        try
        {
            var path = await _filePicker.SaveJsonFileAsync(SafeFileName(snapshot.Name) + "-cv-backup");
            if (path is null)
                return;
            await File.WriteAllTextAsync(path, _decoderCvService.Export(snapshot));
            OperationStatus = "CV backup exported.";
        }
        catch (Exception exception)
        {
            OperationStatus = $"CV export failed: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportCvSnapshotAsync()
    {
        if (_locomotive is null || _decoderCvService is null || _filePicker is null)
            return;
        try
        {
            var path = await _filePicker.BrowseForJsonFileAsync();
            if (path is null)
                return;
            var json = await File.ReadAllTextAsync(path);
            var decoder = _locomotive.Decoder;
            var protocol = decoder?.Protocol ?? DecoderProtocol.Dcc;
            var snapshot = _decoderCvService.Import(json, protocol);
            if (decoder is null)
            {
                decoder = new LocomotiveDecoderProfile { Protocol = protocol };
                _locomotive.Decoder = decoder;
            }
            decoder.CvSnapshots.Add(snapshot);
            DecoderSnapshots.Insert(0, snapshot);
            Passport = _libraryService.BuildPassport(_locomotive);
            await SaveChangesAsync();
            OperationStatus = "CV backup imported.";
        }
        catch (Exception exception)
        {
            OperationStatus = $"CV import failed: {exception.Message}";
        }
    }

    private Task SaveChangesAsync()
        => _projectContext?.SaveSolutionInternalAsync() ?? Task.CompletedTask;

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var safe = new string(value.Where(character => !invalid.Contains(character)).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "locomotive" : safe;
    }
}
