// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed record DecoderCvValidationError(int Row, string Message);

public sealed record DecoderCvChange(int Number, int? PreviousValue, int? CurrentValue)
{
    public string Kind => PreviousValue is null ? "Added" : CurrentValue is null ? "Removed" : "Changed";
}

public interface IDecoderCvService
{
    IReadOnlyList<DecoderCvValidationError> Validate(DecoderProtocol protocol, IReadOnlyList<DecoderCvValue> values);

    void ReplaceSnapshotValues(LocomotiveDecoderProfile profile, Guid snapshotId, IReadOnlyList<DecoderCvValue> values);

    IReadOnlyList<DecoderCvChange> Compare(DecoderCvSnapshot previous, DecoderCvSnapshot current);

    string Export(DecoderCvSnapshot snapshot);

    DecoderCvSnapshot Import(string json, DecoderProtocol protocol);
}

public sealed class DecoderCvService : IDecoderCvService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public IReadOnlyList<DecoderCvValidationError> Validate(DecoderProtocol protocol, IReadOnlyList<DecoderCvValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var errors = new List<DecoderCvValidationError>();
        var seen = new HashSet<int>();
        var maximumCv = protocol == DecoderProtocol.Dcc ? 1024 : 255;

        for (var index = 0; index < values.Count; index++)
        {
            var value = values[index];
            if (value.Number < 1 || value.Number > maximumCv)
                errors.Add(new DecoderCvValidationError(index + 1, $"CV number must be between 1 and {maximumCv}."));
            if (value.Value is < 0 or > 255)
                errors.Add(new DecoderCvValidationError(index + 1, "CV value must be between 0 and 255."));
            if (!seen.Add(value.Number))
                errors.Add(new DecoderCvValidationError(index + 1, $"CV {value.Number} is duplicated."));
        }

        return errors;
    }

    public void ReplaceSnapshotValues(LocomotiveDecoderProfile profile, Guid snapshotId, IReadOnlyList<DecoderCvValue> values)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(values);
        var errors = Validate(profile.Protocol, values);
        if (errors.Count != 0)
            throw new ArgumentException(string.Join(" ", errors.Select(error => $"Row {error.Row}: {error.Message}")), nameof(values));

        var snapshot = profile.CvSnapshots.SingleOrDefault(candidate => candidate.Id == snapshotId)
            ?? throw new KeyNotFoundException($"CV snapshot {snapshotId} was not found.");
        snapshot.Values = values
            .OrderBy(value => value.Number)
            .Select(CloneValue)
            .ToList();
    }

    public IReadOnlyList<DecoderCvChange> Compare(DecoderCvSnapshot previous, DecoderCvSnapshot current)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);
        var previousValues = previous.Values.ToDictionary(value => value.Number, value => value.Value);
        var currentValues = current.Values.ToDictionary(value => value.Number, value => value.Value);

        return previousValues.Keys
            .Union(currentValues.Keys)
            .Order()
            .Select(number => new DecoderCvChange(
                number,
                previousValues.TryGetValue(number, out var previousValue) ? previousValue : null,
                currentValues.TryGetValue(number, out var currentValue) ? currentValue : null))
            .Where(change => change.PreviousValue != change.CurrentValue)
            .ToArray();
    }

    public string Export(DecoderCvSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var copy = CloneSnapshot(snapshot);
        copy.Values = copy.Values.OrderBy(value => value.Number).ToList();
        return JsonSerializer.Serialize(copy, JsonOptions) + Environment.NewLine;
    }

    public DecoderCvSnapshot Import(string json, DecoderProtocol protocol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = JsonSerializer.Deserialize<DecoderCvSnapshot>(json, JsonOptions)
            ?? throw new JsonException("The CV snapshot is empty.");
        var errors = Validate(protocol, snapshot.Values);
        if (errors.Count != 0)
            throw new JsonException(string.Join(" ", errors.Select(error => $"Row {error.Row}: {error.Message}")));
        snapshot.Values = snapshot.Values.OrderBy(value => value.Number).Select(CloneValue).ToList();
        return snapshot;
    }

    private static DecoderCvSnapshot CloneSnapshot(DecoderCvSnapshot source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        CapturedAt = source.CapturedAt,
        Source = source.Source,
        Values = source.Values.Select(CloneValue).ToList()
    };

    private static DecoderCvValue CloneValue(DecoderCvValue source) => new()
    {
        Number = source.Number,
        Value = source.Value,
        Description = source.Description
    };
}
