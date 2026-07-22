// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Recording;

using Moba.Common.Recording;
using System.Text.Json;

/// <summary>
/// Validates one explicitly allow-listed recording payload without binding imported CLR type names.
/// </summary>
public interface IRecordingPayloadValidator
{
    string TypeKey { get; }

    RecordingReplayApplicability ReplayApplicability { get; }

    RecordingPayloadValidationResult Validate(JsonElement payload);
}

/// <summary>
/// Reports whether a known mapper-owned payload satisfies its stable schema.
/// </summary>
public sealed record RecordingPayloadValidationResult(bool IsValid, string? ErrorMessage)
{
    public static RecordingPayloadValidationResult Success() => new(true, null);

    public static RecordingPayloadValidationResult Failure(string errorMessage) => new(false, errorMessage);
}