// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using Domain;
using Domain.Enum;

/// <summary>
/// ViewModel for TTS Announcement actions.
/// Wraps WorkflowAction with typed properties for Message, VoiceName, and Rate.
/// Volume is controlled via system-wide Windows volume settings.
/// </summary>
public sealed class AnnouncementViewModel : WorkflowActionViewModel
{
    #region Fields
    // (No additional fields - inherits from WorkflowActionViewModel)
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="AnnouncementViewModel"/> class for the given workflow action.
    /// </summary>
    /// <param name="action">The underlying workflow action that defines this announcement.</param>
    public AnnouncementViewModel(WorkflowAction action) : base(action, ActionType.Announcement) { }

    /// <summary>
    /// Text to be spoken (supports templates: {JourneyName}, {StationName}).
    /// </summary>
    public string Message
    {
        get => GetParameter<string>("Message") ?? string.Empty;
        set => SetParameter("Message", value);
    }

    /// <summary>
    /// Azure TTS voice name (e.g., "de-DE-KatjaNeural").
    /// </summary>
    public string VoiceName
    {
        get => GetParameter<string>("VoiceName") ?? "de-DE-KatjaNeural";
        set => SetParameter("VoiceName", value);
    }

    /// <summary>
    /// Speech rate in percent relative to the default rate (for example, -50 to +200).
    /// </summary>
    public int Rate
    {
        get => GetParameter<int>("Rate");
        set => SetParameter("Rate", value);
    }

    /// <summary>
    /// Returns a human-readable description of the announcement for debugging and UI display.
    /// </summary>
    /// <returns>A string describing the announcement.</returns>
    public override string ToString() => !string.IsNullOrEmpty(Name) ? $"{Name} (Announcement)" : $"Announcement: {Message}";
}
