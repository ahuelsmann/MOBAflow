// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using Domain;
using Moba.Domain.Enum;

/// <summary>
/// ViewModel for Audio playback actions.
/// Wraps WorkflowAction with typed properties for FilePath, Volume.
/// </summary>
public class AudioViewModel : WorkflowActionViewModel
{
    #region Fields
    // (No additional fields - inherits from WorkflowActionViewModel)
    #endregion

    public AudioViewModel(WorkflowAction action) : base(action, ActionType.Audio) { }

    /// <summary>
    /// Path to audio file (relative or absolute).
    /// </summary>
    public string FilePath
    {
        get => GetParameter<string>("FilePath") ?? string.Empty;
        set => SetParameter("FilePath", value);
    }

    /// <summary>
    /// Volume (0.0 - 1.0).
    /// </summary>
    public double Volume
    {
        get => GetParameter<double>("Volume");
        set => SetParameter("Volume", value);
    }

    /// <summary>
    /// Loop playback.
    /// </summary>
    public bool Loop
    {
        get => GetParameter<bool>("Loop");
        set => SetParameter("Loop", value);
    }

    public override string ToString() => !string.IsNullOrEmpty(Name) ? $"{Name} (Audio)" : $"Audio: {Path.GetFileName(FilePath)}";
}
