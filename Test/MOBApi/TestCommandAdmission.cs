// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBApi;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moba.MOBApi.Service;

/// <summary>Builds the command admission with an optional logger for MOBApi tests.</summary>
internal static class TestCommandAdmission
{
    public static RuntimeCommandAdmission Create(
        IRuntimeCommandQueue queue,
        ISolutionCache solutionCache,
        ILogger<RuntimeCommandAdmission>? logger = null) =>
        new(queue, solutionCache, logger ?? NullLogger<RuntimeCommandAdmission>.Instance);
}
