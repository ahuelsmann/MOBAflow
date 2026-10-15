// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Interface;
using Localization;

/// <summary>Edits app-local UI language with explicit saving and restart semantics.</summary>
public sealed partial class LanguageSettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly ISettingsService _persistence;
    private readonly IUiLocalizer _text;

    /// <summary>Creates language presentation state using existing app settings persistence.</summary>
    public LanguageSettingsViewModel(AppSettings settings, ISettingsService persistence, IUiLocalizer text)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(persistence);
        ArgumentNullException.ThrowIfNull(text);
        _settings = settings;
        _persistence = persistence;
        _text = text;
        Choices = [new(UiLanguagePreference.System, text.GetText("LanguageSystem")),
            new(UiLanguagePreference.English, "English"), new(UiLanguagePreference.German, "Deutsch")];
        SelectedChoice = Choices.First(choice => choice.Value == settings.Application.UiLanguage);
    }

    /// <summary>Available app-local preferences; language names are shown in their native form.</summary>
    public IReadOnlyList<LanguageChoice> Choices { get; }

    [ObservableProperty]
    public partial LanguageChoice? SelectedChoice { get; set; }

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    /// <summary>Persists only the UI preference and leaves the active language unchanged.</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedChoice is null) return;
        var previous = _settings.Application.UiLanguage;
        _settings.Application.UiLanguage = SelectedChoice.Value;
        try
        {
            await _persistence.SaveSettingsAsync(_settings).ConfigureAwait(true);
            Message = _text.GetText("LanguageRestart");
        }
        catch (IOException)
        {
            _settings.Application.UiLanguage = previous;
            Message = _text.GetText("LanguageSaveFailed");
        }
        catch (UnauthorizedAccessException)
        {
            _settings.Application.UiLanguage = previous;
            Message = _text.GetText("LanguageSaveFailed");
        }
    }
}

/// <summary>A stable preference value and its presentation name.</summary>
public sealed record LanguageChoice(string Value, string Name);
