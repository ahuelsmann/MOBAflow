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
public partial class AudioViewModel : WorkflowActionViewModel
{
    #region Fields
    private readonly IIoService _ioService;
    private readonly ISoundPlayer? _soundPlayer;
    #endregion

    public AudioViewModel(WorkflowAction action, IIoService ioService, ISoundPlayer? soundPlayer = null) : base(action, ActionType.Audio)
    {
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

    public override string ToString() => !string.IsNullOrEmpty(Name) ? $"{Name} (Audio)" : $"Audio: {Path.GetFileName(FilePath)}";
}
