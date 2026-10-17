// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Interface;

using Microsoft.Extensions.Logging;

using Sound;

/// <summary>
/// Services the solution, project and workflow view models need beyond their model:
/// the file picker for audio actions, sound preview and logging.
/// </summary>
/// <param name="FilePicker">Picker used by audio actions to choose a sound file.</param>
/// <param name="SoundPlayer">Optional preview player for audio actions.</param>
/// <param name="LoggerFactory">Optional logger factory for the view models.</param>
public sealed record SolutionViewModelServices(
    IFilePickerService FilePicker,
    ISoundPlayer? SoundPlayer = null,
    ILoggerFactory? LoggerFactory = null);
