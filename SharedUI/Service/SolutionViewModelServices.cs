// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Interface;

using Microsoft.Extensions.Logging;

using Sound;

/// <summary>
/// Services the solution, project and workflow view models need beyond their model:
/// the file picker for audio actions, the preview player and logging. Hosts without a capability register
/// the null object (<see cref="NullFilePickerService"/>, <see cref="NullSoundPlayer"/>).
/// </summary>
/// <param name="FilePicker">Picker used by audio actions to choose a sound file.</param>
/// <param name="SoundPlayer">Preview player for audio actions.</param>
/// <param name="LoggerFactory">Logger factory for the view models.</param>
public sealed record SolutionViewModelServices(
    IFilePickerService FilePicker,
    ISoundPlayer SoundPlayer,
    ILoggerFactory LoggerFactory);
