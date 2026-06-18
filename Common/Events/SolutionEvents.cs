// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Events;

/// <summary>
/// Published when MOBAsmart has fetched and applied a solution from MOBApi.
/// </summary>
public sealed record SolutionSyncedEvent(DateTimeOffset UpdatedAt, string SolutionName, string? ActiveProjectName) : EventBase;