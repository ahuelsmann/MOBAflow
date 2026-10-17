// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Common.Configuration;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Localization;
using Moba.SharedUI.ViewModel;
using Moq;
using System.Globalization;
using System.Text.Json;

internal sealed partial class SettingsPageViewModelTests
{
    [TestCase("en-US", "en-US")]
    [TestCase("system", "de-DE")]
    public async Task LanguagePickerFollowsResetBeforeTheNextExplicitSave(string savedPreference, string selectedPreference)
    {
        var (settingsViewModel, settings, persistence, dispatcher) = Create();
        settings.Application.UiLanguage = savedPreference;
        using var languageViewModel = new LanguageSettingsViewModel(settings, persistence.Object,
            new UiLocalizer(CultureInfo.GetCultureInfo("en-US")), dispatcher.Object);
        languageViewModel.SelectedChoice = languageViewModel.Choices.Single(choice => choice.Value == selectedPreference);
        persistence.Setup(service => service.ResetToDefaultsAsync()).Returns(() =>
        {
            settings.Application.UiLanguage = UiLanguagePreference.System;
            persistence.Raise(service => service.UiLanguagePreferenceChanged += null, EventArgs.Empty);
            return Task.CompletedTask;
        });
        string? writtenPreference = null;
        persistence.Setup(service => service.SaveUiLanguageAsync(It.IsAny<string>())).Callback<string>(preference =>
            writtenPreference = preference).Returns(Task.CompletedTask);

        await settingsViewModel.ResetToDefaultsCommand.ExecuteAsync(null).ConfigureAwait(false);
        var choiceAfterReset = languageViewModel.SelectedChoice?.Value;
        await languageViewModel.SaveCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(choiceAfterReset, Is.EqualTo(UiLanguagePreference.System));
            Assert.That(writtenPreference, Is.EqualTo(UiLanguagePreference.System));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PendingFailedLanguageSaveDoesNotLeakIntoAnotherSettingsSave(bool accessDenied)
    {
        var settings = new AppSettings();
        var pendingWrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var persistence = new Mock<ISettingsService>();
        string? automaticSavePreference = null;
        persistence.Setup(service => service.SaveUiLanguageAsync(UiLanguagePreference.English)).Returns(pendingWrite.Task);
        persistence.Setup(service => service.SaveSettingsAsync(settings))
            .Returns(() =>
            {
                automaticSavePreference = settings.Application.UiLanguage;
                return Task.CompletedTask;
            });
        var text = new UiLocalizer(CultureInfo.GetCultureInfo("de-DE"));
        using var viewModel = new LanguageSettingsViewModel(settings, persistence.Object, text, CreateLanguageDispatcher());
        viewModel.SelectedChoice = viewModel.Choices.Single(choice => choice.Value == UiLanguagePreference.English);

        var save = viewModel.SaveCommand.ExecuteAsync(null);
        var preferenceWhilePending = settings.Application.UiLanguage;
        await persistence.Object.SaveSettingsAsync(settings).ConfigureAwait(false);
        pendingWrite.SetException(accessDenied ? new UnauthorizedAccessException("denied") : new IOException("failed"));
        await save.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(preferenceWhilePending, Is.EqualTo(UiLanguagePreference.System));
            Assert.That(automaticSavePreference, Is.EqualTo(UiLanguagePreference.System));
            Assert.That(settings.Application.UiLanguage, Is.EqualTo(UiLanguagePreference.System));
            Assert.That(viewModel.Message, Is.EqualTo(text.GetText("LanguageSaveFailed")));
        }
    }

    [TestCase("system")]
    [TestCase("en-US")]
    [TestCase("de-DE")]
    public async Task SavePersistsPreferenceWithoutChangingActiveLanguageOrOtherSettings(string preference)
    {
        var settings = new AppSettings();
        settings.Speech.Language = "fr-FR";
        settings.Speech.VoiceName = "user voice";
        settings.Speech.TestMessage = "User announcement";
        settings.Application.LastSolutionPath = "user-solution.json";
        var before = JsonSerializer.Serialize(settings);
        var text = new UiLocalizer(CultureInfo.GetCultureInfo("en-US"));
        var persistence = new Mock<ISettingsService>();
        persistence.Setup(service => service.SaveUiLanguageAsync(preference))
            .Callback(() => settings.Application.UiLanguage = preference).Returns(Task.CompletedTask);
        using var viewModel = new LanguageSettingsViewModel(settings, persistence.Object, text, CreateLanguageDispatcher());
        viewModel.SelectedChoice = viewModel.Choices.Single(choice => choice.Value == preference);

        await viewModel.SaveCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.Application.UiLanguage, Is.EqualTo(preference));
            Assert.That(text.Culture.Name, Is.EqualTo("en-US"));
            Assert.That(viewModel.Message, Does.Contain("Restart"));
        }
        persistence.Verify(service => service.SaveUiLanguageAsync(preference), Times.Once);
        persistence.Verify(service => service.SaveSettingsAsync(It.IsAny<AppSettings>()), Times.Never);
        settings.Application.UiLanguage = UiLanguagePreference.System;
        Assert.That(JsonSerializer.Serialize(settings), Is.EqualTo(before));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task FailedSaveRestoresPreferenceAndCanBeRetriedInTheActiveLanguage(bool accessDenied)
    {
        var settings = new AppSettings();
        var text = new UiLocalizer(CultureInfo.GetCultureInfo("de-DE"));
        var persistence = new Mock<ISettingsService>();
        persistence.SetupSequence(service => service.SaveUiLanguageAsync(UiLanguagePreference.English))
            .ThrowsAsync(accessDenied
                ? new UnauthorizedAccessException("private path must not be displayed")
                : new IOException("private path must not be displayed"))
            .Returns(() =>
            {
                settings.Application.UiLanguage = UiLanguagePreference.English;
                return Task.CompletedTask;
            });
        using var viewModel = new LanguageSettingsViewModel(settings, persistence.Object, text, CreateLanguageDispatcher());
        viewModel.SelectedChoice = viewModel.Choices.Single(choice => choice.Value == "en-US");

        await viewModel.SaveCommand.ExecuteAsync(null).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.Application.UiLanguage, Is.EqualTo("system"));
            Assert.That(viewModel.Message, Is.EqualTo(text.GetText("LanguageSaveFailed")));
            Assert.That(viewModel.Message, Does.Not.Contain("private path"));
        }
        await viewModel.SaveCommand.ExecuteAsync(null).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.Application.UiLanguage, Is.EqualTo("en-US"));
            Assert.That(viewModel.Message, Is.EqualTo(text.GetText("LanguageRestart")));
        }
    }

    [Test]
    public void SelectionAloneDoesNotWriteSettings()
    {
        var settings = new AppSettings();
        var persistence = new Mock<ISettingsService>();
        using var viewModel = new LanguageSettingsViewModel(settings, persistence.Object,
            new UiLocalizer(CultureInfo.GetCultureInfo("en-US")), CreateLanguageDispatcher());
        viewModel.SelectedChoice = viewModel.Choices[2];
        Assert.That(settings.Application.UiLanguage, Is.EqualTo("system"));
        persistence.Verify(service => service.SaveUiLanguageAsync(It.IsAny<string>()), Times.Never);
        persistence.Verify(service => service.SaveSettingsAsync(It.IsAny<AppSettings>()), Times.Never);
    }

    private static IUiDispatcher CreateLanguageDispatcher()
    {
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(candidate => candidate.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        return dispatcher.Object;
    }

    [Test]
    public void CommittedLanguageRefreshUsesUiDispatcherAndStopsAfterDisposal()
    {
        var settings = new AppSettings();
        var persistence = new Mock<ISettingsService>();
        var dispatcher = new Mock<IUiDispatcher>();
        var actions = new List<Action>();
        dispatcher.Setup(candidate => candidate.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(actions.Add);
        using var viewModel = new LanguageSettingsViewModel(settings, persistence.Object,
            new UiLocalizer(CultureInfo.GetCultureInfo("de-DE")), dispatcher.Object);
        viewModel.SelectedChoice = viewModel.Choices.Single(choice => choice.Value == UiLanguagePreference.English);
        viewModel.Message = "Previous status";

        persistence.Raise(service => service.UiLanguagePreferenceChanged += null, EventArgs.Empty);
        Assert.That(viewModel.SelectedChoice?.Value, Is.EqualTo(UiLanguagePreference.English));
        actions.Single()();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.SelectedChoice?.Value, Is.EqualTo(UiLanguagePreference.System));
            Assert.That(viewModel.Message, Is.Empty);
        }
        viewModel.Dispose();
        settings.Application.UiLanguage = UiLanguagePreference.German;
        persistence.Raise(service => service.UiLanguagePreferenceChanged += null, EventArgs.Empty);
        Assert.That(actions, Has.Count.EqualTo(1));
    }
}
