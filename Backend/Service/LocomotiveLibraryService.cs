// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;

/// <summary>
/// UI-independent row used by desktop, mobile, print and future API presentations.
/// </summary>
public sealed record LocomotiveLibraryEntry(
    Guid LocomotiveId,
    string Name,
    uint? DigitalAddress,
    string? Manufacturer,
    string? ArticleNumber,
    bool HasDecoderProfile,
    bool HasMaintenanceHistory);

public sealed record LocomotiveDecoderSummary(
    string? Manufacturer,
    string? Model,
    string? FirmwareVersion,
    DecoderProtocol Protocol);

public sealed record LocomotiveMaintenanceSummary(
    DateTimeOffset PerformedAt,
    MaintenanceCategory Category,
    string Description);

/// <summary>
/// Structured passport content. It intentionally contains neither URLs nor QR payloads.
/// The locomotive ID is the only navigation identity; filesystem paths and transport URLs are deliberately excluded.
/// </summary>
public sealed record LocomotivePassport(
    Guid LocomotiveId,
    string Name,
    uint? DigitalAddress,
    string? Manufacturer,
    string? ArticleNumber,
    LocomotiveDecoderSummary? Decoder,
    LocomotiveMaintenanceSummary? LatestMaintenance,
    int DecoderSnapshotCount,
    MaintenanceDueState? MaintenanceState);

public interface ILocomotiveLibraryService
{
    IReadOnlyList<LocomotiveLibraryEntry> BuildLibrary(Project project);

    LocomotivePassport BuildPassport(Locomotive locomotive);
}

/// <summary>
/// Projects the persisted locomotive aggregate into presentation-neutral library data.
/// </summary>
public sealed class LocomotiveLibraryService : ILocomotiveLibraryService
{
    private readonly ILocomotiveMaintenanceService _maintenanceService;
    private readonly TimeProvider _timeProvider;

    public LocomotiveLibraryService(
        ILocomotiveMaintenanceService? maintenanceService = null,
        TimeProvider? timeProvider = null)
    {
        _maintenanceService = maintenanceService ?? new LocomotiveMaintenanceService();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public IReadOnlyList<LocomotiveLibraryEntry> BuildLibrary(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return project.Locomotives
            .Select(locomotive => new LocomotiveLibraryEntry(
                locomotive.Id,
                locomotive.Name,
                locomotive.DigitalAddress,
                locomotive.Manufacturer,
                locomotive.ArticleNumber,
                locomotive.Decoder is not null,
                locomotive.Maintenance?.Entries.Count > 0))
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.LocomotiveId)
            .ToArray();
    }

    public LocomotivePassport BuildPassport(Locomotive locomotive)
    {
        ArgumentNullException.ThrowIfNull(locomotive);

        var decoder = locomotive.Decoder is null
            ? null
            : new LocomotiveDecoderSummary(
                locomotive.Decoder.Manufacturer,
                locomotive.Decoder.Model,
                locomotive.Decoder.FirmwareVersion,
                locomotive.Decoder.Protocol);

        var latestEntry = locomotive.Maintenance?.Entries
            .OrderByDescending(entry => entry.PerformedAt)
            .ThenBy(entry => entry.Id)
            .FirstOrDefault();
        var maintenance = latestEntry is null
            ? null
            : new LocomotiveMaintenanceSummary(
                latestEntry.PerformedAt,
                latestEntry.Category,
                latestEntry.Description);
        var maintenanceState = locomotive.Maintenance is { Plans.Count: > 0 } maintenanceData
            && _maintenanceService.Validate(maintenanceData).Count == 0
                ? _maintenanceService.Evaluate(maintenanceData, _timeProvider.GetUtcNow())
                    .Select(status => status.State)
                    .DefaultIfEmpty(MaintenanceDueState.NotScheduled)
                    .Max()
                : null;

        return new LocomotivePassport(
            locomotive.Id,
            locomotive.Name,
            locomotive.DigitalAddress,
            locomotive.Manufacturer,
            locomotive.ArticleNumber,
            decoder,
            maintenance,
            locomotive.Decoder?.CvSnapshots.Count ?? 0,
            maintenanceState);
    }
}
