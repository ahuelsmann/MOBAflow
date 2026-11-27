// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
#if WINDOWS
namespace Moba.Test.WinUI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moba.SharedUI.Configuration;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Desktop.ViewModel;
using Moba.Speech.Base;
using Moba.Test.Helpers;
using Moba.MOBAflow.Service;
using Moq;

internal sealed partial class NavigationItemFactoryTests
{
    private static readonly string[] HistoryWithFallback = ["solution", "recorder", "monitor"];
    private static readonly string[] HistoryOnlyDisabled = ["monitor", "recorder"];

    [Test]
    public void FeatureTogglePageProviderExcludesOverviewWhileKeepingAllToggleablePages()
    {
        var pages = NavigationRegistration.RegisterPages(new ServiceCollection());
        var provider = new FeatureTogglePageProvider(pages, new UiSettings());

        var toggleablePages = provider.GetToggleablePages();
        var overviewRegistration = pages.Single(page => page.Tag == "overview");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(overviewRegistration.FeatureToggleKey, Is.EqualTo(FeatureTogglePages.Overview));
            Assert.That(toggleablePages, Has.Count.EqualTo(18));
            Assert.That(toggleablePages.Select(page => page.FeatureToggleKey), Is.EquivalentTo(FeatureToggleRegistry.ToggleablePageKeys));
            Assert.That(toggleablePages.Select(page => page.FeatureToggleKey), Does.Not.Contain(FeatureTogglePages.Overview));
        }
    }

    [Test]
    public async Task FeatureToggleChangesUpdateNavigationAvailabilityAndSeparatorsImmediatelyWithoutRestart()
    {
        var pages = NavigationRegistration.RegisterPages(new ServiceCollection());
        var settings = new UiSettings();
        var provider = new FeatureTogglePageProvider(pages, settings);
        var persistence = new Mock<ISettingsService>();
        persistence.Setup(service => service.GetSettings()).Returns(settings);
        persistence.Setup(service => service.SaveSettingsAsync(settings)).Returns(Task.CompletedTask);
        persistence.Setup(service => service.ResetToDefaultsAsync()).Returns(() =>
        {
            settings.FeatureToggles.Pages.Clear();
            return Task.CompletedTask;
        });

        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(candidate => candidate.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        var viewModel = new SettingsPageViewModel(
            settings,
            persistence.Object,
            TestLayouts.Create(persistence.Object),
            dispatcher.Object,
            NullLogger<SettingsPageViewModel>.Instance,
            provider,
            new NullSpeechOutput());

        NavigationMenuVisibility? latestVisibility = MainWindowNavigationBootstrapper.ComputeMenuVisibility(pages, settings.FeatureToggles);
        var notificationCount = 0;
        viewModel.FeatureTogglesChanged += (_, _) =>
        {
            notificationCount++;
            latestVisibility = MainWindowNavigationBootstrapper.ComputeMenuVisibility(pages, settings.FeatureToggles);
        };

        var trainControlToggle = viewModel.FeatureToggleItems.Single(item => item.FeatureToggleKey == FeatureTogglePages.TrainControl);
        var monitorToggle = viewModel.FeatureToggleItems.Single(item => item.FeatureToggleKey == FeatureTogglePages.Monitor);
        var recorderToggle = viewModel.FeatureToggleItems.Single(item => item.FeatureToggleKey == FeatureTogglePages.Recorder);

        // Disable the only page in TrainControl category: separator before TrainControl collapses while separator before Journey stays visible.
        trainControlToggle.IsChecked = false;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(notificationCount, Is.EqualTo(1));
            Assert.That(latestVisibility!.PageVisibilityByTag["overview"], Is.True);
            Assert.That(latestVisibility.PageVisibilityByTag["traincontrol"], Is.False);
            Assert.That(latestVisibility.SeparatorVisibility[0], Is.False);
            Assert.That(latestVisibility.SeparatorVisibility[1], Is.True);
        }

        // Disable all pages in Monitoring category: separator before Monitoring collapses while separator before Help stays visible.
        monitorToggle.IsChecked = false;
        recorderToggle.IsChecked = false;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(notificationCount, Is.EqualTo(3));
            Assert.That(latestVisibility!.PageVisibilityByTag["monitor"], Is.False);
            Assert.That(latestVisibility.PageVisibilityByTag["recorder"], Is.False);
            Assert.That(latestVisibility.SeparatorVisibility[4], Is.False);
            Assert.That(latestVisibility.SeparatorVisibility[5], Is.True);
        }

        // Re-enabling TrainControl restores both item and separator live.
        trainControlToggle.IsChecked = true;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(notificationCount, Is.EqualTo(4));
            Assert.That(latestVisibility!.PageVisibilityByTag["traincontrol"], Is.True);
            Assert.That(latestVisibility.SeparatorVisibility[0], Is.True);
        }

        // Resetting defaults restores Monitor & Recorder and notifies live without restart.
        await viewModel.ResetToDefaultsCommand.ExecuteAsync(null).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(notificationCount, Is.EqualTo(5));
            Assert.That(latestVisibility!.PageVisibilityByTag["monitor"], Is.True);
            Assert.That(latestVisibility.PageVisibilityByTag["recorder"], Is.True);
            Assert.That(latestVisibility.SeparatorVisibility.All(visible => visible), Is.True);
        }
    }

    [Test]
    public void FeatureToggleFallbackRedirectsOnlyWhenActivePageBecomesDisabled()
    {
        var pages = NavigationRegistration.RegisterPages(new ServiceCollection());
        var toggles = new FeatureToggleSettings();
        toggles.Pages[FeatureTogglePages.Overview] = new FeatureTogglePageSettings { IsAvailable = false };
        toggles.Pages[FeatureTogglePages.Monitor] = new FeatureTogglePageSettings { IsAvailable = false };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(MainWindowNavigationBootstrapper.ResolveFallbackPageTagIfCurrentDisabled(pages, toggles, "monitor"), Is.EqualTo("overview"));
            Assert.That(MainWindowNavigationBootstrapper.ResolveFallbackPageTagIfCurrentDisabled(pages, toggles, "settings"), Is.Null);
            Assert.That(MainWindowNavigationBootstrapper.ResolveFallbackPageTagIfCurrentDisabled(pages, toggles, "help"), Is.Null);
            Assert.That(MainWindowNavigationBootstrapper.ResolveFallbackPageTagIfCurrentDisabled(pages, toggles, "info"), Is.Null);
            Assert.That(MainWindowNavigationBootstrapper.ResolveFallbackPageTagIfCurrentDisabled(pages, toggles, "overview"), Is.Null);
            Assert.That(MainWindowNavigationBootstrapper.ResolveFallbackPageTagIfCurrentDisabled(pages, toggles, "recorder"), Is.Null);
            Assert.That(MainWindowNavigationBootstrapper.ResolveFallbackPageTagIfCurrentDisabled(pages, toggles, null), Is.Null);
        }
    }

    [Test]
    public void FeatureToggleNavigationServiceHelpersBlockDisabledTargetsAndSkipDisabledHistory()
    {
        var pages = NavigationRegistration.RegisterPages(new ServiceCollection());
        var toggles = new FeatureToggleSettings();
        toggles.Pages[FeatureTogglePages.Overview] = new FeatureTogglePageSettings { IsAvailable = false };
        toggles.Pages[FeatureTogglePages.Monitor] = new FeatureTogglePageSettings { IsAvailable = false };
        toggles.Pages[FeatureTogglePages.Recorder] = new FeatureTogglePageSettings { IsAvailable = false };

        var historyWithFallback = new Stack<string>(HistoryWithFallback);
        var historyOnlyDisabled = new Stack<string>(HistoryOnlyDisabled);

        using (Assert.EnterMultipleScope())
        {
            Assert.Throws<InvalidOperationException>(() => NavigationService.ResolveAvailablePage(pages, toggles, "monitor"));
            Assert.That(NavigationService.ResolveAvailablePage(pages, toggles, "overview").Tag, Is.EqualTo("overview"));
            Assert.That(NavigationService.HasAvailableHistoryEntry(historyWithFallback, pages, toggles), Is.True);
            Assert.That(NavigationService.PopNextAvailableHistoryPage(historyWithFallback, pages, toggles)?.Tag, Is.EqualTo("solution"));
            Assert.That(NavigationService.HasAvailableHistoryEntry(historyOnlyDisabled, pages, toggles), Is.False);
            Assert.That(NavigationService.PopNextAvailableHistoryPage(historyOnlyDisabled, pages, toggles), Is.Null);
        }
    }
}
#endif
