// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;



using Common.Runtime;

using Common.Validation;



using Domain;



using Interface;



using Microsoft.Extensions.Logging;



using System.Text.Json;



/// <summary>

/// File-based cache for MOBAsmart solution and signal-box data under a platform-provided directory.

/// </summary>

public sealed class MobileSolutionStore : IMobileSolutionStore

{

    private const int SolutionSchemaVersion = 1;



    private static readonly JsonSerializerOptions SignalBoxJsonOptions = new()

    {

        PropertyNameCaseInsensitive = true,

        WriteIndented = true

    };



    private readonly string _storageDirectory;

    private readonly ILogger<MobileSolutionStore> _logger;

    private readonly SemaphoreSlim _writeLock = new(1, 1);



    public MobileSolutionStore(string storageDirectory, ILogger<MobileSolutionStore> logger)

    {

        ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);

        ArgumentNullException.ThrowIfNull(logger);



        _storageDirectory = storageDirectory;

        _logger = logger;

    }



    /// <inheritdoc />

    public async Task SaveAsync(Solution solution, SolutionSyncMeta meta, CancellationToken cancellationToken = default)

    {

        ArgumentNullException.ThrowIfNull(solution);

        ArgumentNullException.ThrowIfNull(meta);



        var solutionJson = JsonSerializer.Serialize(solution, JsonOptions.Default);

        var validationResult = JsonValidationService.Validate(solutionJson, SolutionSchemaVersion);

        if (!validationResult.IsValid)

        {

            _logger.LogWarning("Mobile solution cache save skipped: {Error}", validationResult.ErrorMessage);

            return;

        }



        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try

        {

            Directory.CreateDirectory(_storageDirectory);



            await WriteTextAtomicAsync(

                GetSolutionPath(),

                solutionJson,

                cancellationToken).ConfigureAwait(false);



            var metaJson = JsonSerializer.Serialize(meta, JsonOptions.Compact);

            await WriteTextAtomicAsync(

                GetMetaPath(),

                metaJson,

                cancellationToken).ConfigureAwait(false);



            _logger.LogDebug(

                "Cached mobile solution {SolutionName} updated at {UpdatedAt}",

                meta.SolutionName,

                meta.UpdatedAt);

        }

        finally

        {

            _writeLock.Release();

        }

    }



    /// <inheritdoc />

    public async Task SaveSignalBoxElementsAsync(

        IReadOnlyList<SignalBoxElementRuntimeSnapshot> elements,

        CancellationToken cancellationToken = default)

    {

        ArgumentNullException.ThrowIfNull(elements);



        if (elements.Count == 0)

        {

            return;

        }



        var json = JsonSerializer.Serialize(elements, SignalBoxJsonOptions);



        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try

        {

            Directory.CreateDirectory(_storageDirectory);

            await WriteTextAtomicAsync(GetSignalBoxPath(), json, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Cached {Count} mobile signal-box element(s)", elements.Count);

        }

        finally

        {

            _writeLock.Release();

        }

    }



    /// <inheritdoc />
    public async Task SaveLocomotiveFleetAsync(
        IReadOnlyList<LocomotiveFleetSnapshot> fleet,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fleet);

        if (fleet.Count == 0)
        {
            return;
        }

        var json = JsonSerializer.Serialize(fleet, SignalBoxJsonOptions);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(_storageDirectory);
            await WriteTextAtomicAsync(GetLocomotiveFleetPath(), json, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Cached {Count} mobile locomotive fleet item(s)", fleet.Count);
        }
        finally
        {
            _writeLock.Release();
        }
    }



    /// <inheritdoc />

    public async Task<MobileSolutionCacheEntry?> TryLoadAsync(CancellationToken cancellationToken = default)

    {

        var solutionPath = GetSolutionPath();

        var metaPath = GetMetaPath();



        if (!File.Exists(solutionPath) || !File.Exists(metaPath))

        {

            return null;

        }



        try

        {

            var solutionJson = await File.ReadAllTextAsync(solutionPath, cancellationToken).ConfigureAwait(false);

            var validationResult = JsonValidationService.Validate(solutionJson, SolutionSchemaVersion);

            if (!validationResult.IsValid)

            {

                _logger.LogWarning("Cached mobile solution invalid: {Error}", validationResult.ErrorMessage);

                return null;

            }



            var solution = JsonSerializer.Deserialize<Solution>(solutionJson, JsonOptions.Default);

            if (solution == null || solution.Projects.Count == 0)

            {

                _logger.LogWarning("Cached mobile solution deserialized empty or without projects");

                return null;

            }



            var metaJson = await File.ReadAllTextAsync(metaPath, cancellationToken).ConfigureAwait(false);

            var meta = JsonSerializer.Deserialize<SolutionSyncMeta>(metaJson, JsonOptions.Compact);

            if (meta == null)

            {

                _logger.LogWarning("Cached mobile solution meta is missing or invalid");

                return null;

            }



            var signalBoxElements = await TryLoadSignalBoxElementsAsync(cancellationToken).ConfigureAwait(false)
                ?? [];

            var locomotiveFleet = await TryLoadLocomotiveFleetAsync(cancellationToken).ConfigureAwait(false)
                ?? [];

            return new MobileSolutionCacheEntry(solution, meta, signalBoxElements, locomotiveFleet);

        }

        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)

        {

            _logger.LogDebug(ex, "Failed to load cached mobile solution");

            return null;

        }

    }



    private async Task<IReadOnlyList<SignalBoxElementRuntimeSnapshot>?> TryLoadSignalBoxElementsAsync(

        CancellationToken cancellationToken)

    {

        var signalBoxPath = GetSignalBoxPath();

        if (!File.Exists(signalBoxPath))

        {

            return null;

        }



        try

        {

            var json = await File.ReadAllTextAsync(signalBoxPath, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<List<SignalBoxElementRuntimeSnapshot>>(json, SignalBoxJsonOptions);

        }

        catch (Exception ex) when (ex is IOException or JsonException)

        {

            _logger.LogDebug(ex, "Failed to load cached mobile signal-box snapshot");

            return null;

        }

    }



    private async Task<IReadOnlyList<LocomotiveFleetSnapshot>?> TryLoadLocomotiveFleetAsync(
        CancellationToken cancellationToken)
    {
        var fleetPath = GetLocomotiveFleetPath();
        if (!File.Exists(fleetPath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(fleetPath, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<LocomotiveFleetSnapshot>>(json, SignalBoxJsonOptions);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _logger.LogDebug(ex, "Failed to load cached mobile locomotive fleet");
            return null;
        }
    }



    private string GetSolutionPath() => Path.Combine(_storageDirectory, "mobile-solution.json");



    private string GetMetaPath() => Path.Combine(_storageDirectory, "mobile-solution-meta.json");



    private string GetSignalBoxPath() => Path.Combine(_storageDirectory, "mobile-signalbox-snapshot.json");

    private string GetLocomotiveFleetPath() => Path.Combine(_storageDirectory, "mobile-locomotive-fleet.json");



    private static async Task WriteTextAtomicAsync(string targetPath, string content, CancellationToken cancellationToken)

    {

        var tempPath = targetPath + ".tmp";

        await File.WriteAllTextAsync(tempPath, content, cancellationToken).ConfigureAwait(false);



        if (File.Exists(targetPath))

        {

            File.Delete(targetPath);

        }



        File.Move(tempPath, targetPath);

    }

}

