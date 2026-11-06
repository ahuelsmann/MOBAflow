// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Common.Configuration;
using Moba.Common.Navigation;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moq;
using System.Diagnostics.CodeAnalysis;

[TestFixture]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "NUnit creates this fixture through reflection.")]
internal sealed class SettingsPageViewModelTests
{
    private static readonly string[] ExpectedLanguages = ["de_DE", "en_US"];
    private static readonly string[] ExpectedVoices = ["en_US-test-low"];

    [Test]
    public void SettingChangesPreserveNormalizationAndDoNotSaveUnchangedValues()
    {
        var (viewModel, settings, persistence, _) = Create();
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        viewModel.DisplayIpAddress = "  192.0.2.10  ";
        viewModel.DisplayIpAddress = "192.0.2.10";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.Display.Esp32IpAddress, Is.EqualTo("192.0.2.10"));
            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(notifications.Single(), Is.EqualTo(nameof(SettingsPageViewModel.DisplayIpAddress)));
        }
        persistence.Verify(service => service.SaveSettingsAsync(settings), Times.Once);
    }

    [Test]
    public void SentenceSilencePreservesToleranceAndClamping()
    {
        var (viewModel, settings, persistence, _) = Create();
        viewModel.PiperSentenceSilenceSeconds = 1.5;
        viewModel.PiperSentenceSilenceSeconds = 1.50001;
        viewModel.PiperSentenceSilenceSeconds = 5;
        viewModel.PiperSentenceSilenceSeconds = 3;

        Assert.That(settings.Speech.Piper.SentenceSilenceSeconds, Is.EqualTo(2));
        persistence.Verify(service => service.SaveSettingsAsync(settings), Times.Exactly(2));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void SignalPolarityChangesOnlyTheChosenOffsetAndNotifiesAndSavesOnce(int offset)
    {
        var (viewModel, settings, persistence, _) = Create();
        var before = GetPolarities(settings);
        var expected = (bool[])before.Clone();
        expected[offset] = !before[offset];
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);
        Action<bool> update = offset switch
        {
            0 => value => viewModel.InvertPolarityOffset0Setting = value,
            1 => value => viewModel.InvertPolarityOffset1Setting = value,
            2 => value => viewModel.InvertPolarityOffset2Setting = value,
            _ => value => viewModel.InvertPolarityOffset3Setting = value
        };

        update(expected[offset]);
        update(expected[offset]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(GetPolarities(settings), Is.EqualTo(expected));
            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(notifications.Single(), Is.EqualTo($"InvertPolarityOffset{offset}Setting"));
        }
        persistence.Verify(service => service.SaveSettingsAsync(settings), Times.Once);
    }

    private static bool[] GetPolarities(AppSettings settings) =>
    [
        settings.SignalBox.InvertPolarityOffset0,
        settings.SignalBox.InvertPolarityOffset1,
        settings.SignalBox.InvertPolarityOffset2,
        settings.SignalBox.InvertPolarityOffset3
    ];

    [Test]
    public void FeedbackCountNotifiesOnlyForActualValidChanges()
    {
        var (viewModel, settings, persistence, _) = Create();
        settings.Counter.CountOfFeedbackPoints = 7;
        var changes = 0;
        viewModel.FeedbackPointCountChanged += (_, _) => changes++;
        viewModel.CountOfFeedbackPoints = double.NaN;
        viewModel.CountOfFeedbackPoints = -1;
        viewModel.CountOfFeedbackPoints = 7;
        viewModel.CountOfFeedbackPoints = 3;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.CountOfFeedbackPoints, Is.EqualTo(3));
            Assert.That(changes, Is.EqualTo(1));
        }
        persistence.Verify(service => service.SaveSettingsAsync(settings), Times.Once);
    }

    [Test]
    public void SpeechAndThemeChangesNotifySharedConsumersAndPersistOnce()
    {
        var (viewModel, settings, persistence, _) = Create();
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);
        viewModel.SelectedSpeechProviderId = "piper";
        viewModel.SelectedSpeechProviderId = "piper";
        viewModel.IsDarkMode = false;
        viewModel.IsDarkMode = false;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.IsPiperSpeechProviderSelected, Is.True);
            Assert.That(settings.Application.IsDarkMode, Is.False);
            Assert.That(notifications, Does.Contain(nameof(SettingsPageViewModel.IsPiperSpeechProviderSelected)));
            Assert.That(notifications.Count(name => name == nameof(SettingsPageViewModel.IsDarkMode)), Is.EqualTo(1));
        }
        persistence.Verify(service => service.SaveSettingsAsync(settings), Times.Exactly(2));
    }

    [Test]
    public void FeatureToggleUsesRegisteredMetadataAndUpdatesAvailabilityWithoutExtraSaves()
    {
        var provider = new Mock<IFeatureTogglePageProvider>();
        provider.Setup(candidate => candidate.GetToggleablePages())
            .Returns((FeatureTogglePageInfo[])[new FeatureTogglePageInfo("Overview", nameof(FeatureToggleSettings.IsOverviewPageAvailable), null, NavigationCategory.Monitoring, 10)]);
        var (viewModel, settings, persistence, _) = Create(provider: provider.Object);
        var toggle = viewModel.FeatureToggleItems.Single();
        toggle.IsChecked = false;
        toggle.IsChecked = false;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toggle.CheckBoxContent, Is.EqualTo("Overview"));
            Assert.That(viewModel.IsOverviewPageAvailable, Is.False);
            Assert.That(settings.FeatureToggles.IsOverviewPageAvailable, Is.False);
        }
        persistence.Verify(service => service.SaveSettingsAsync(settings), Times.Once);
    }

    [Test]
    public void SectionLayoutPreservesKeysExpansionUsageAndOrderWithoutNoOpSaves()
    {
        var (viewModel, settings, persistence, _) = Create();
        viewModel.SettingsSectionSortMode = " FREQUENCY ";
        viewModel.SettingsSectionSortMode = "frequency";
        viewModel.SetSettingsPageSectionIsExpanded("Speech", true);
        viewModel.SetSettingsPageSectionIsExpanded("Speech", true);
        viewModel.MarkSettingsPageSectionUsed("Speech");
        viewModel.SetSettingsPageSectionOrder((string[])["Speech", "Display"]);
        viewModel.SetSettingsPageSectionOrder((string[])["Speech", "Display"]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.Layout.SettingsPage.SortMode, Is.EqualTo(SettingsSectionSortModes.Frequency));
            Assert.That(viewModel.GetSettingsPageSectionIsExpanded("Speech"), Is.True);
            Assert.That(viewModel.GetSettingsPageSectionState("Speech").UsageCount, Is.EqualTo(1));
            Assert.That(viewModel.GetSettingsPageSectionState("Speech").LastUsedUtc, Is.Not.Null);
            Assert.That(viewModel.GetSettingsPageSectionState("Display").Order, Is.EqualTo(1));
        }
        persistence.Verify(service => service.SaveSettingsAsync(settings), Times.Exactly(4));
    }

    [Test]
    public async Task SpeechTestPassesUserTextToExistingHostIntegration()
    {
        string? spokenText = null;
        var (viewModel, _, _, _) = Create(speech: message =>
        {
            spokenText = message;
            return Task.CompletedTask;
        });
        viewModel.SpeechTestMessage = "Gleis zwei";
        await viewModel.TestSpeechCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(spokenText, Is.EqualTo("Gleis zwei"));
            Assert.That(viewModel.ShowSuccessMessage, Is.True);
            Assert.That(viewModel.ShowErrorMessage, Is.False);
        }
    }

    [Test]
    public async Task SpeechTestFailureReportsErrorThroughUiDispatcher()
    {
        var (viewModel, _, _, dispatcher) = Create(speech: _ => Task.FromException(new InvalidOperationException("speech unavailable")));
        await viewModel.TestSpeechCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.ErrorMessage, Is.EqualTo("speech unavailable"));
            Assert.That(viewModel.ShowErrorMessage, Is.True);
        }
        dispatcher.Verify(candidate => candidate.InvokeOnUi(It.IsAny<Action>()), Times.Exactly(2));
    }

    [Test]
    public async Task ResetDefaultsFailureKeepsErrorVisible()
    {
        var (viewModel, _, persistence, _) = Create();
        persistence.Setup(service => service.ResetToDefaultsAsync())
            .Returns(Task.FromException(new InvalidOperationException("write failed")));
        await viewModel.ResetToDefaultsCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.ShowErrorMessage, Is.True);
            Assert.That(viewModel.ErrorMessage, Is.EqualTo("write failed"));
            Assert.That(viewModel.ShowSuccessMessage, Is.False);
        }
    }

    [Test]
    public async Task ResetLayoutsPreservesTabVisibilityAndResetsOnlyLayoutData()
    {
        var (viewModel, settings, persistence, _) = Create();
        var visibility = settings.Layout.TabVisibility;
        settings.Layout.ColumnWidths["JourneysMainColumnStarValue"] = 4;
        viewModel.DisplayIpAddress = "192.0.2.10";
        await viewModel.ResetLayoutsCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.Layout.TabVisibility, Is.SameAs(visibility));
            Assert.That(settings.Layout.ColumnWidths, Is.Empty);
            Assert.That(settings.Display.Esp32IpAddress, Is.EqualTo("192.0.2.10"));
            Assert.That(viewModel.IsLayoutResetSuccessful, Is.True);
            Assert.That(viewModel.LayoutResetTitle, Is.EqualTo("Layouts reset"));
        }
        persistence.Verify(service => service.SaveSettingsAsync(settings), Times.Exactly(2));
    }

    [Test]
    public async Task ResetLayoutsFailurePreservesExistingErrorMessage()
    {
        var (viewModel, settings, persistence, _) = Create();
        persistence.Setup(service => service.SaveSettingsAsync(settings))
            .Returns(Task.FromException(new InvalidOperationException("layout write failed")));
        await viewModel.ResetLayoutsCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.IsLayoutResetSuccessful, Is.False);
            Assert.That(viewModel.LayoutResetTitle, Is.EqualTo("Layout reset failed"));
            Assert.That(viewModel.LayoutResetMessage, Is.EqualTo("layout write failed"));
        }
    }

    [Test]
    public async Task ResetDefaultsRefreshesBindingsAndFeatureItemsAfterPersistence()
    {
        var provider = new Mock<IFeatureTogglePageProvider>();
        provider.Setup(candidate => candidate.GetToggleablePages())
            .Returns((FeatureTogglePageInfo[])[new FeatureTogglePageInfo("Overview", nameof(FeatureToggleSettings.IsOverviewPageAvailable), null, NavigationCategory.Monitoring, 10)]);
        var (viewModel, settings, persistence, _) = Create(provider: provider.Object);
        viewModel.DisplayPort = 12345;
        viewModel.FeatureToggleItems.Single().IsChecked = false;
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);
        persistence.Invocations.Clear();
        persistence.Setup(service => service.ResetToDefaultsAsync()).Returns(async () =>
        {
            await Task.Yield();
            settings.Display.Port = new AppSettings().Display.Port;
            settings.FeatureToggles.IsOverviewPageAvailable = true;
        });

        await viewModel.ResetToDefaultsCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.DisplayPort, Is.EqualTo(new AppSettings().Display.Port));
            Assert.That(viewModel.FeatureToggleItems.Single().IsChecked, Is.True);
            Assert.That(viewModel.ShowSuccessMessage, Is.False);
            Assert.That(viewModel.ShowErrorMessage, Is.False);
            Assert.That(notifications, Does.Contain(nameof(SettingsPageViewModel.DisplayPort)));
            Assert.That(notifications, Does.Contain(nameof(SettingsPageViewModel.IsOverviewPageAvailable)));
        }
        persistence.Verify(service => service.SaveSettingsAsync(It.IsAny<AppSettings>()), Times.Never);
    }

    [Test]
    public void PiperVoiceSelectionPreservesLanguageFilteringModelAndOptionalConfig()
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"rf24-voices-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(directory);
        try
        {
            var germanModel = System.IO.Path.Combine(directory, "de_DE-test-medium.onnx");
            var englishModel = System.IO.Path.Combine(directory, "en_US-test-low.onnx");
            System.IO.File.WriteAllText(germanModel, string.Empty);
            System.IO.File.WriteAllText(germanModel + ".json", "{}");
            System.IO.File.WriteAllText(englishModel, string.Empty);
            var (viewModel, settings, _, _) = Create();
            viewModel.PiperModelPath = germanModel;
            viewModel.SelectedPiperLanguage = "en_US";
            viewModel.SelectedPiperVoiceName = "en_US-test-low";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.AvailablePiperLanguages, Is.EquivalentTo(ExpectedLanguages));
                Assert.That(viewModel.AvailablePiperVoices, Is.EqualTo(ExpectedVoices));
                Assert.That(settings.Speech.Piper.ModelPath, Is.EqualTo(englishModel));
                Assert.That(settings.Speech.Piper.ConfigPath, Is.Empty);
                Assert.That(viewModel.VoiceName, Is.EqualTo("en_US-test-low"));
            }
        }
        finally
        {
            System.IO.Directory.Delete(directory, recursive: true);
        }
    }

    private static (SettingsPageViewModel ViewModel, AppSettings Settings, Mock<ISettingsService> Persistence, Mock<IUiDispatcher> Dispatcher)
        Create(Func<string, Task>? speech = null, IFeatureTogglePageProvider? provider = null)
    {
        var settings = new AppSettings();
        var persistence = new Mock<ISettingsService>();
        persistence.Setup(service => service.SaveSettingsAsync(settings)).Returns(Task.CompletedTask);
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(candidate => candidate.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        return (new SettingsPageViewModel(settings, persistence.Object, dispatcher.Object,
            NullLogger<SettingsPageViewModel>.Instance, provider, speech), settings, persistence, dispatcher);
    }
}
