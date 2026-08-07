#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBAflow;

using Moba.Common.Configuration;
using Moba.WinUI.Service;

using Microsoft.Extensions.DependencyInjection;

[TestFixture]
internal sealed class FeatureTogglePageProviderTests
{
    [Test]
    public void GetToggleablePages_ShouldCoverEveryRegisteredFeaturePage()
    {
        // Arrange
        var pages = NavigationRegistration.RegisterPages(new ServiceCollection());
        var provider = new FeatureTogglePageProvider(pages, new AppSettings());
        string[] alwaysAvailablePageTags = ["help", "info", "settings"];

        // Act
        var pagesWithoutToggle = pages
            .Where(page => string.IsNullOrWhiteSpace(page.FeatureToggleKey))
            .Select(page => page.Tag)
            .ToArray();
        var toggleablePages = provider.GetToggleablePages();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(pagesWithoutToggle, Is.EquivalentTo(alwaysAvailablePageTags));
            Assert.That(toggleablePages.Select(page => page.Title), Does.Contain("Stations"));
            Assert.That(toggleablePages.Select(page => page.Title), Does.Contain("Recorder"));
        });
    }
}
#endif