// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;

using System.Text.Json;

/// <summary>
/// Durable absolute counters for one vehicle. Absolute values make recovery idempotent.
/// </summary>
public sealed record VehicleUsageCheckpoint(long OperatingSeconds, long CompletedTrips);

/// <summary>
/// Durable vehicle-usage state for one project.
/// </summary>
public sealed class VehicleUsageCheckpointState
{
    public Guid ProjectId { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public Dictionary<Guid, VehicleUsageCheckpoint> Vehicles { get; init; } = [];

    public HashSet<Guid> CompletedJourneyRuns { get; init; } = [];
}

public interface IVehicleUsageCheckpointStore
{
    VehicleUsageCheckpointState? Load(Guid projectId);

    void Save(VehicleUsageCheckpointState state);
}

public sealed class NullVehicleUsageCheckpointStore : IVehicleUsageCheckpointStore
{
    public VehicleUsageCheckpointState? Load(Guid projectId) => null;

    public void Save(VehicleUsageCheckpointState state)
    {
    }
}

/// <summary>
/// Atomically persists usage checkpoints independently from the editable solution file.
/// </summary>
public sealed class FileVehicleUsageCheckpointStore : IVehicleUsageCheckpointStore
{
    private readonly object _lock = new();
    private readonly string _path;

    public FileVehicleUsageCheckpointStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MOBAflow",
            "vehicle-usage-checkpoints.json"))
    {
    }

    public FileVehicleUsageCheckpointStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)
            ?? throw new ArgumentException("Checkpoint path requires a directory.", nameof(path)));
    }

    public VehicleUsageCheckpointState? Load(Guid projectId)
    {
        lock (_lock)
        {
            return Read().GetValueOrDefault(projectId);
        }
    }

    public void Save(VehicleUsageCheckpointState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        lock (_lock)
        {
            var data = Read();
            data[state.ProjectId] = state;
            var temporaryPath = _path + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(data, JsonOptions.Compact));
            File.Move(temporaryPath, _path, true);
        }
    }

    private Dictionary<Guid, VehicleUsageCheckpointState> Read()
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<Guid, VehicleUsageCheckpointState>>(
                       File.ReadAllText(_path),
                       JsonOptions.Compact)
                   ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
