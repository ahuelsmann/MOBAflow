// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Security;

internal sealed record CompatibilityStatusResponse(
    CompatibilityReadTelemetry Telemetry,
    CompatibilityReadMigrationStatus Readiness);
