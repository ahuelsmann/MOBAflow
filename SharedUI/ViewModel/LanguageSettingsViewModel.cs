// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Interface;
using Localization;

/// <summary>Edits app-local UI language with explicit saving and restart semantics.</summary>
public sealed partial class LanguageSettingsViewModel : ObservableObject, IDisposable
{
    private readonly AppSettings _settings;
    private readonly ISettingsService _persistence;
    private readonly IUiLocalizer _text;
    private readonly IUiDispatcher _dispatcher;

    /// <summary>Creates language presentation state using existing app settings persistence.</summary>
    public LanguageSettingsViewModel(AppSettings settings, ISettingsService persistence, IUiLocalizer text, IUiDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(persistence);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _settings = settings;
        _persistence = persistence;
        _text = text;
        _dispatcher = dispatcher;
        Choices = [new(UiLanguagePreference.System, text.GetText("LanguageSystem")),
            new(UiLanguagePreference.English, "English"), new(UiLanguagePreference.German, "Deutsch")];
        SelectedChoice = Choices.First(choice => choice.Value == settings.Application.UiLanguage);
        _persistence.UiLanguagePreferenceChanged += OnUiLanguagePreferenceChanged;
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
        try
        {
            await _persistence.SaveUiLanguageAsync(SelectedChoice.Value).ConfigureAwait(true);
            Message = _text.GetText("LanguageRestart");
        }
        catch (IOException)
        {
            Message = _text.GetText("LanguageSaveFailed");
        }
        catch (UnauthorizedAccessException)
        {
            Message = _text.GetText("LanguageSaveFailed");
        }
    }

    private void OnUiLanguagePreferenceChanged(object? sender, EventArgs args)
    {
        _ = sender;
        _ = args;
        _dispatcher.InvokeOnUi(() =>
        {
            SelectedChoice = Choices.First(choice => choice.Value == _settings.Application.UiLanguage);
            Message = string.Empty;
        });
    }

    /// <summary>Releases the subscription to the app-local settings service.</summary>
    public void Dispose()
    {
        _persistence.UiLanguagePreferenceChanged -= OnUiLanguagePreferenceChanged;
        GC.SuppressFinalize(this);
    }
}

/// <summary>A stable preference value and its presentation name.</summary>
public sealed record LanguageChoice(string Value, string Name);
