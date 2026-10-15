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
        _settings = settings;
        _persistence = persistence;
        _text = text;
        Choices = [new(UiLanguagePreference.System, text.Get("LanguageSystem")),
            new(UiLanguagePreference.English, "English"), new(UiLanguagePreference.German, "Deutsch")];
        _selectedChoice = Choices.First(choice => choice.Value == settings.Application.UiLanguage);
    }

    /// <summary>Available app-local preferences; language names are shown in their native form.</summary>
    public IReadOnlyList<LanguageChoice> Choices { get; }

    [ObservableProperty]
    private LanguageChoice? _selectedChoice;

    [ObservableProperty]
    private string _message = string.Empty;

    /// <summary>Persists only the UI preference and leaves the active language unchanged.</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedChoice is null) return;
        var previous = _settings.Application.UiLanguage;
        _settings.Application.UiLanguage = SelectedChoice.Value;
        try
        {
            await _persistence.SaveSettingsAsync(_settings);
            Message = _text.Get("LanguageRestart");
        }
        catch (Exception)
        {
            _settings.Application.UiLanguage = previous;
            Message = _text.Get("LanguageSaveFailed");
        }
    }
}

/// <summary>A stable preference value and its presentation name.</summary>
public sealed record LanguageChoice(string Value, string Name);
