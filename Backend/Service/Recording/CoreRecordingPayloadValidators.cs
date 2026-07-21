// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Recording;

using Moba.Common.Recording;
using System.Text.Json;

internal static class CoreRecordingPayloadValidators
{
    public static IReadOnlyList<IRecordingPayloadValidator> Create() =>
    [
        Schema("z21.connection.established", ("connected", IsBoolean)),
        Schema("z21.connection.lost", ("connected", IsBoolean)),
        Schema("z21.track-power.changed", ("isOn", IsBoolean)),
        Schema(
            "z21.xbus-status.changed",
            ("emergencyStop", IsBoolean),
            ("trackOff", IsBoolean),
            ("shortCircuit", IsBoolean),
            ("programming", IsBoolean)),
        Schema(
            "z21.system-state.changed",
            ("mainCurrent", IsInt32),
            ("progCurrent", IsInt32),
            ("filteredMainCurrent", IsInt32),
            ("temperature", IsInt32),
            ("supplyVoltage", IsInt32),
            ("vccVoltage", IsInt32),
            ("centralState", IsInt32),
            ("centralStateEx", IsInt32)),
        Schema("z21.feedback.activated", ("inPort", IsPositiveInt32)),
        Schema(
            "z21.signal-aspect.changed",
            ("signalId", IsInt32),
            ("aspect", IsBoundedString),
            ("previousAspect", IsNullableBoundedString)),
        Schema(
            "z21.switch-position.changed",
            ("switchId", IsInt32),
            ("isLeft", IsBoolean),
            ("previousPosition", IsNullableBoolean)),
        Schema(
            "runtime.state.changed",
            ("isConnected", IsBoolean),
            ("isTrackPowerOn", IsBoolean),
            ("isZ21Connecting", IsBoolean),
            ("isManualDisconnectRequested", IsBoolean),
            ("isEmergencyStopActive", IsBoolean),
            ("isShortCircuitActive", IsBoolean),
            ("isProgrammingModeActive", IsBoolean),
            ("isOperatorAckRequired", IsBoolean)),
        Schema(
            "journey.transition",
            ("projectId", IsGuid),
            ("journeyId", IsGuid),
            ("journeyRunId", IsGuid),
            ("kind", IsJourneyTransitionKind),
            ("feedbackIndex", IsNonNegativeInt32),
            ("currentOccurrence", IsUInt32),
            ("requiredOccurrences", IsUInt32),
            ("inPort", IsNullablePositiveInt32),
            ("stationId", IsNullableGuid),
            ("stationIndex", IsStationIndex),
            ("isActive", IsBoolean))
    ];

    private static IRecordingPayloadValidator Schema(
        string typeKey,
        params (string Name, Func<JsonElement, bool> Validate)[] properties) =>
        new RecordingPayloadSchemaValidator(typeKey, properties);

    private static bool IsBoolean(JsonElement value) =>
        value.ValueKind is JsonValueKind.True or JsonValueKind.False;

    private static bool IsNullableBoolean(JsonElement value) =>
        value.ValueKind == JsonValueKind.Null || IsBoolean(value);

    private static bool IsInt32(JsonElement value) => value.TryGetInt32(out _);

    private static bool IsNonNegativeInt32(JsonElement value) =>
        value.TryGetInt32(out var number) && number >= 0;

    private static bool IsPositiveInt32(JsonElement value) =>
        value.TryGetInt32(out var number) && number > 0;

    private static bool IsNullablePositiveInt32(JsonElement value) =>
        value.ValueKind == JsonValueKind.Null || IsPositiveInt32(value);

    private static bool IsStationIndex(JsonElement value) =>
        value.TryGetInt32(out var number) && number >= -1;

    private static bool IsUInt32(JsonElement value) => value.TryGetUInt32(out _);

    private static bool IsGuid(JsonElement value) =>
        value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var id) && id != Guid.Empty;

    private static bool IsNullableGuid(JsonElement value) =>
        value.ValueKind == JsonValueKind.Null || IsGuid(value);

    private static bool IsBoundedString(JsonElement value) =>
        value.ValueKind == JsonValueKind.String &&
        value.GetString() is { Length: > 0 and <= RecordingFormat.MaxTypeKeyLength };

    private static bool IsNullableBoundedString(JsonElement value) =>
        value.ValueKind == JsonValueKind.Null || IsBoundedString(value);

    private static bool IsJourneyTransitionKind(JsonElement value) =>
        value.ValueKind == JsonValueKind.String &&
        Enum.TryParse<Moba.Common.Events.JourneyRuntimeTransitionKind>(value.GetString(), ignoreCase: false, out _);

    private sealed class RecordingPayloadSchemaValidator : IRecordingPayloadValidator
    {
        private readonly IReadOnlyDictionary<string, Func<JsonElement, bool>> _properties;

        public RecordingPayloadSchemaValidator(
            string typeKey,
            IEnumerable<(string Name, Func<JsonElement, bool> Validate)> properties)
        {
            TypeKey = typeKey;
            _properties = properties.ToDictionary(property => property.Name, property => property.Validate, StringComparer.Ordinal);
        }

        public string TypeKey { get; }

        public RecordingReplayApplicability ReplayApplicability => RecordingReplayApplicability.ReplayApplicable;

        public RecordingPayloadValidationResult Validate(JsonElement payload)
        {
            if (payload.ValueKind != JsonValueKind.Object)
            {
                return RecordingPayloadValidationResult.Failure("Payload must be an object.");
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in payload.EnumerateObject())
            {
                if (!seen.Add(property.Name))
                {
                    return RecordingPayloadValidationResult.Failure($"Payload property '{property.Name}' is duplicated.");
                }

                if (!_properties.TryGetValue(property.Name, out var validate) || !validate(property.Value))
                {
                    return RecordingPayloadValidationResult.Failure($"Payload property '{property.Name}' is unknown or invalid.");
                }
            }

            var missingProperty = _properties.Keys.FirstOrDefault(property => !seen.Contains(property));
            return missingProperty is null
                ? RecordingPayloadValidationResult.Success()
                : RecordingPayloadValidationResult.Failure($"Payload property '{missingProperty}' is required.");
        }
    }
}

internal sealed class CoreRecordingPayloadValidatorRegistrationMarker;