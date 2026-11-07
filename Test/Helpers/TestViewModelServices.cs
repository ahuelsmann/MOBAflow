// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.SharedUI.Service;
using Moba.Sound;

/// <summary>
/// The services the solution, project and workflow view models receive in tests: no file picker, a silent sound
/// player and no logging.
/// </summary>
internal static class TestViewModelServices
{
    public static SolutionViewModelServices Default { get; } =
        new(new NullFilePickerService(), new NullSoundPlayer(), NullLoggerFactory.Instance);
}
