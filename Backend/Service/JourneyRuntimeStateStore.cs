// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Domain;
using System.Text.Json;

public sealed record JourneyRuntimeCheckpoint(int CurrentFeedbackIndex, uint CurrentStepOccurrence);

public interface IJourneyRuntimeStateStore
{
    JourneyRuntimeCheckpoint? Load(Guid projectId, Guid journeyId);
    void Save(Guid projectId, JourneySessionState state);
    void Reset(Guid projectId, Guid journeyId);
}

public sealed class NullJourneyRuntimeStateStore : IJourneyRuntimeStateStore
{
    public JourneyRuntimeCheckpoint? Load(Guid projectId, Guid journeyId) => null;
    public void Save(Guid projectId, JourneySessionState state) { }
    public void Reset(Guid projectId, Guid journeyId) { }
}

/// <summary>Persists journey sequence progress independently from editable solution data.</summary>
public sealed class FileJourneyRuntimeStateStore : IJourneyRuntimeStateStore
{
    private readonly object _lock = new();
    private readonly string _path;

    public FileJourneyRuntimeStateStore()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MOBAflow");
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "journey-runtime-state.json");
    }

    public JourneyRuntimeCheckpoint? Load(Guid projectId, Guid journeyId)
    {
        lock (_lock) return Read().GetValueOrDefault(Key(projectId, journeyId));
    }

    public void Save(Guid projectId, JourneySessionState state)
    {
        lock (_lock)
        {
            var data = Read();
            data[Key(projectId, state.JourneyId)] = new(state.CurrentFeedbackIndex, state.CurrentStepOccurrence);
            Write(data);
        }
    }

    public void Reset(Guid projectId, Guid journeyId)
    {
        lock (_lock)
        {
            var data = Read();
            if (data.Remove(Key(projectId, journeyId))) Write(data);
        }
    }

    private Dictionary<string, JourneyRuntimeCheckpoint> Read()
    {
        if (!File.Exists(_path)) return [];
        try { return JsonSerializer.Deserialize<Dictionary<string, JourneyRuntimeCheckpoint>>(File.ReadAllText(_path), JsonOptions.Compact) ?? []; }
        catch (JsonException) { return []; }
    }

    private void Write(Dictionary<string, JourneyRuntimeCheckpoint> data)
    {
        var temporaryPath = _path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(data, JsonOptions.Compact));
        File.Move(temporaryPath, _path, true);
    }

    private static string Key(Guid projectId, Guid journeyId) => $"{projectId:N}:{journeyId:N}";
}
