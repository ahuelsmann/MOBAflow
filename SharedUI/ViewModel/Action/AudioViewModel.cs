// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using CommunityToolkit.Mvvm.Input;
using Domain;
using Domain.Enum;
using Interface;
using Sound;

/// <summary>
/// ViewModel for Audio playback actions.
/// Wraps WorkflowAction with typed properties for FilePath.
/// Volume is controlled via system-wide Windows volume settings.
/// Audio files are played once when the action is triggered.
/// </summary>
public sealed partial class AudioViewModel : WorkflowActionViewModel
{
    #region Fields
    private readonly IIoService _ioService;
    private readonly ISoundPlayer? _soundPlayer;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioViewModel"/> class for the given workflow action.
    /// </summary>
    /// <param name="action">The underlying workflow action that defines this audio playback.</param>
    /// <param name="ioService">Service used to browse for audio files.</param>
    /// <param name="soundPlayer">Optional sound player used to preview the selected audio file.</param>
    public AudioViewModel(WorkflowAction action, IIoService ioService, ISoundPlayer? soundPlayer = null) : base(action, ActionType.Audio)
    {
        ArgumentNullException.ThrowIfNull(ioService);
        _ioService = ioService;
        _soundPlayer = soundPlayer;
    }

    /// <summary>
    /// Path to audio file (relative or absolute).
    /// </summary>
    public string FilePath
    {
        get => GetParameter<string>("FilePath") ?? string.Empty;
        set => SetParameter("FilePath", value);
    }

    /// <summary>
    /// Command to browse for an audio file.
    /// </summary>
    [RelayCommand]
    private async Task BrowseForFileAsync()
    {
        var path = await _ioService.BrowseForAudioFileAsync();
        if (!string.IsNullOrEmpty(path))
        {
            FilePath = path;
        }
    }

    /// <summary>
    /// Command to preview/play the audio file.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPlayAudio))]
    private async Task PlayAudioAsync()
    {
        if (_soundPlayer != null && !string.IsNullOrWhiteSpace(FilePath))
        {
            await _soundPlayer.PlayAsync(FilePath);
        }
    }

    private bool CanPlayAudio() => _soundPlayer != null && !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);

    /// <summary>
    /// Returns a human-readable description of the audio action for debugging and UI display.
    /// </summary>
    /// <returns>A string describing the audio playback action.</returns>
    public override string ToString() => !string.IsNullOrEmpty(Name) ? $"{Name} (Audio)" : $"Audio: {Path.GetFileName(FilePath)}";
}
