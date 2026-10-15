// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Common.Configuration;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Localization;
using Moba.SharedUI.ViewModel;
using Moq;
using System.Globalization;
using System.Text.Json;

[TestFixture]
internal sealed class LanguageSettingsViewModelTests
{
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
        persistence.Setup(service => service.SaveSettingsAsync(settings)).Returns(Task.CompletedTask);
        var viewModel = new LanguageSettingsViewModel(settings, persistence.Object, text);
        viewModel.SelectedChoice = viewModel.Choices.Single(choice => choice.Value == preference);

        await viewModel.SaveCommand.ExecuteAsync(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.Application.UiLanguage, Is.EqualTo(preference));
            Assert.That(text.Culture.Name, Is.EqualTo("en-US"));
            Assert.That(viewModel.Message, Does.Contain("Restart"));
        }
        persistence.Verify(service => service.SaveSettingsAsync(settings), Times.Once);
        settings.Application.UiLanguage = UiLanguagePreference.System;
        Assert.That(JsonSerializer.Serialize(settings), Is.EqualTo(before));
    }

    [Test]
    public async Task FailedSaveRestoresPreferenceAndCanBeRetriedInTheActiveLanguage()
    {
        var settings = new AppSettings();
        var text = new UiLocalizer(CultureInfo.GetCultureInfo("de-DE"));
        var persistence = new Mock<ISettingsService>();
        persistence.SetupSequence(service => service.SaveSettingsAsync(settings))
            .ThrowsAsync(new IOException("private path must not be displayed"))
            .Returns(Task.CompletedTask);
        var viewModel = new LanguageSettingsViewModel(settings, persistence.Object, text);
        viewModel.SelectedChoice = viewModel.Choices.Single(choice => choice.Value == "en-US");

        await viewModel.SaveCommand.ExecuteAsync(null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.Application.UiLanguage, Is.EqualTo("system"));
            Assert.That(viewModel.Message, Is.EqualTo(text.Get("LanguageSaveFailed")));
            Assert.That(viewModel.Message, Does.Not.Contain("private path"));
        }
        await viewModel.SaveCommand.ExecuteAsync(null);
        Assert.That(settings.Application.UiLanguage, Is.EqualTo("en-US"));
        Assert.That(viewModel.Message, Is.EqualTo(text.Get("LanguageRestart")));
    }

    [Test]
    public void SelectionAloneDoesNotWriteSettings()
    {
        var settings = new AppSettings();
        var persistence = new Mock<ISettingsService>(MockBehavior.Strict);
        var viewModel = new LanguageSettingsViewModel(settings, persistence.Object,
            new UiLocalizer(CultureInfo.GetCultureInfo("en-US")));
        viewModel.SelectedChoice = viewModel.Choices[2];
        Assert.That(settings.Application.UiLanguage, Is.EqualTo("system"));
        persistence.VerifyNoOtherCalls();
    }
}
