// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.SharedUI.Interface;
using Moba.SharedUI.Mobile;

/// <summary>
/// Creates the solution synchronizer for mobile ViewModel tests.
/// </summary>
internal static class TestSolutionSynchronizers
{
    public static MobileSolutionSynchronizer Create(ISolutionRemoteLoader? solutionRemoteLoader = null) =>
        new(NullLogger<MobileSolutionSynchronizer>.Instance, solutionRemoteLoader);
}
