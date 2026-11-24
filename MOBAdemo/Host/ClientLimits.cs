// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Demo;

/// <summary>Per-client request budgets of the demo host (FR-056).</summary>
/// <param name="NonStaticPerMinute">Requests per minute that are not static files.</param>
/// <param name="StaticPerHour">Static-file requests per hour.</param>
internal sealed record HostLimits(int NonStaticPerMinute = 60, int StaticPerHour = 200);
