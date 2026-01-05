// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using CommunityToolkit.Mvvm.Input;
using Domain;
using Domain.Enum;
using Interface;

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
    #endregion

    public AudioViewModel(WorkflowAction action, IIoService ioService) : base(action, ActionType.Audio) 
    {
        _ioService = ioService;
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
        var path = await _ioService.BrowseForAudioFileAsync().ConfigureAwait(false);
        if (!string.IsNullOrEmpty(path))
        {
            FilePath = path;
        }
    }

    public override string ToString() => !string.IsNullOrEmpty(Name) ? $"{Name} (Audio)" : $"Audio: {Path.GetFileName(FilePath)}";
}
