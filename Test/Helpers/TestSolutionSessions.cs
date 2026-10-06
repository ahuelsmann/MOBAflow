// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;

/// <summary>
/// Creates solution sessions for ViewModel tests with the same file access and dispatcher as the ViewModel.
/// </summary>
internal static class TestSolutionSessions
{
    public static SolutionSession Create(
        Solution solution,
        IUiDispatcher uiDispatcher,
        IConnectionRuntime runtimeConnection,
        IIoService? ioService = null) =>
        new(
            solution,
            ioService ?? new NullIoService(),
            uiDispatcher,
            runtimeConnection,
            NullLogger<SolutionSession>.Instance);
}
