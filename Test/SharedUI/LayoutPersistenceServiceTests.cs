// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moba.Common.Configuration;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moba.Test.Helpers;
using Moq;
using System.Text.Json;

/// <summary>
/// The single layout persistence boundary: existing keys, defaults, column widths, Settings sections, reset and
/// logged write failures.
/// </summary>
[TestFixture]
internal sealed class LayoutPersistenceServiceTests
{
    private AppSettings _settings = null!;
    private Mock<ISettingsService> _persistence = null!;

    [SetUp]
    public void SetUp()
    {
        _settings = new AppSettings();
        _persistence = new Mock<ISettingsService>();
        _persistence.Setup(service => service.GetSettings()).Returns(_settings);
        _persistence.Setup(service => service.SaveSettingsAsync(_settings)).Returns(Task.CompletedTask);
    }

    private LayoutPersistenceService Create(ILogger<LayoutPersistenceService>? logger = null) =>
        TestLayouts.Create(_persistence.Object, logger);

    [Test]
    public void Constructor_RequiresSettingsColumnWidthsAndLogger()
    {
        var logger = NullLogger<LayoutPersistenceService>.Instance;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => new LayoutPersistenceService(null!, new LayoutColumnWidthsViewModel(), logger),
                Throws.ArgumentNullException);
            Assert.That(() => new LayoutPersistenceService(_persistence.Object, null!, logger),
                Throws.ArgumentNullException);
            Assert.That(() => new LayoutPersistenceService(_persistence.Object, new LayoutColumnWidthsViewModel(), null!),
                Throws.ArgumentNullException);
        }
    }

    [Test]
    public void Constructor_LoadsSavedColumnWidthsOverThePageDefaultsWithoutSaving()
    {
        _settings.Layout.ColumnWidths["JourneysPage:0"] = 320;
        _settings.Layout.LocomotivesPage.ListColumnWidth = 275;

        var layout = Create();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(layout.Layout, Is.SameAs(_settings.Layout));
            Assert.That(layout.ColumnWidths.GetColumnWidth("JourneysPage", 0), Is.EqualTo(320), "saved width");
            Assert.That(layout.ColumnWidths.GetColumnWidth("JourneysPage", 2), Is.EqualTo(250), "page default");
            Assert.That(layout.ColumnWidths.GetColumnWidth("LocomotivesPage", 0), Is.EqualTo(275), "structured fallback");
        }
        _persistence.Verify(service => service.SaveSettingsAsync(It.IsAny<AppSettings>()), Times.Never);
    }

    [Test]
    public void Persist_WritesTheExistingKeyAndSavesOnce()
    {
        var layout = Create();

        layout.Persist(saved => saved.SolutionPage.IsProjectListExpanded = false);

        Assert.That(_settings.Layout.SolutionPage.IsProjectListExpanded, Is.False);
        _persistence.Verify(service => service.SaveSettingsAsync(_settings), Times.Once);
    }

    [Test]
    public async Task Persist_FailureIsLoggedAndKeepsTheChange()
    {
        var logged = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var logger = new Mock<ILogger<LayoutPersistenceService>>();
        logger.Setup(value => value.IsEnabled(LogLevel.Warning)).Returns(true);
        logger.Setup(value => value.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation => logged.TrySetResult((Exception?)invocation.Arguments[3])));
        var failure = new IOException("settings file locked");
        _persistence.Setup(service => service.SaveSettingsAsync(_settings)).Returns(Task.FromException(failure));
        var layout = Create(logger.Object);

        layout.Persist(saved => saved.MonitorPage.IsTrafficExpanded = false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_settings.Layout.MonitorPage.IsTrafficExpanded, Is.False);
            Assert.That(await logged.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false), Is.SameAs(failure));
        }
    }

    [Test]
    public async Task SaveAsync_ReportsAFailedWriteToTheCaller()
    {
        _persistence.Setup(service => service.SaveSettingsAsync(_settings))
            .Returns(Task.FromException(new IOException("settings file locked")));
        var layout = Create();

        await Assert.ThrowsAsync<IOException>(() => layout.SaveAsync()).ConfigureAwait(false);
    }

    [Test]
    public void PanelAndColumnState_SurvivesASaveAndRestoreUnderTheExistingKeys()
    {
        var layout = Create();
        layout.Persist(saved =>
        {
            saved.SolutionPage.IsProjectListExpanded = false;
            saved.SolutionPage.PropertiesColumnStarValue = 2.5;
            saved.SignalBoxPage.ToolboxColumnWidth = 260;
            saved.ColumnWidths["LocomotivesPage:0"] = 310;
        });

        var json = JsonSerializer.Serialize(_settings, JsonOptions.Default);
        var restored = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions.Default)!;
        var restoredLayout = TestLayouts.Create(restored);

        using var document = JsonDocument.Parse(json);
        var saved = document.RootElement.GetProperty("layout");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(saved.GetProperty("solutionPage").GetProperty("isProjectListExpanded").GetBoolean(), Is.False);
            Assert.That(saved.GetProperty("solutionPage").GetProperty("propertiesColumnStarValue").GetDouble(), Is.EqualTo(2.5));
            Assert.That(saved.GetProperty("signalBoxPage").GetProperty("toolboxColumnWidth").GetDouble(), Is.EqualTo(260));
            Assert.That(saved.GetProperty("columnWidths").GetProperty("LocomotivesPage:0").GetDouble(), Is.EqualTo(310));
            Assert.That(restoredLayout.Layout.SolutionPage.IsProjectListExpanded, Is.False);
            Assert.That(restoredLayout.Layout.SolutionPage.PropertiesColumnStarValue, Is.EqualTo(2.5));
            Assert.That(restoredLayout.Layout.SignalBoxPage.ToolboxColumnWidth, Is.EqualTo(260));
            Assert.That(restoredLayout.ColumnWidths.GetColumnWidth("LocomotivesPage", 0), Is.EqualTo(310));
        }
    }

    [Test]
    public async Task ResetAsync_ReplacesTheLayoutKeepsTabVisibilityAndSaves()
    {
        var layout = Create();
        var visibility = _settings.Layout.TabVisibility;
        var before = _settings.Layout;
        _settings.Layout.ColumnWidths["JourneysPage:0"] = 320;
        _settings.Layout.SolutionPage.IsProjectListExpanded = false;

        await layout.ResetAsync().ConfigureAwait(false);
        layout.Persist(saved => saved.SignalBoxPage.IsToolboxExpanded = false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_settings.Layout, Is.Not.SameAs(before));
            Assert.That(layout.Layout, Is.SameAs(_settings.Layout), "later writes follow the new layout");
            Assert.That(_settings.Layout.TabVisibility, Is.SameAs(visibility));
            Assert.That(_settings.Layout.ColumnWidths, Is.Empty);
            Assert.That(_settings.Layout.SolutionPage.IsProjectListExpanded, Is.True);
            Assert.That(_settings.Layout.SignalBoxPage.IsToolboxExpanded, Is.False);
            Assert.That(before.SignalBoxPage.IsToolboxExpanded, Is.True);
        }
        _persistence.Verify(service => service.SaveSettingsAsync(_settings), Times.Exactly(2));
    }

    [Test]
    public async Task ResetAsync_ReportsAFailedWriteToTheCaller()
    {
        _persistence.Setup(service => service.SaveSettingsAsync(_settings))
            .Returns(Task.FromException(new InvalidOperationException("layout write failed")));
        var layout = Create();

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => layout.ResetAsync()).ConfigureAwait(false);
        Assert.That(failure?.Message, Is.EqualTo("layout write failed"));
    }

    [Test]
    public void SettingsSections_PreserveKeysExpansionUsageAndOrderWithoutNoOpSaves()
    {
        var layout = Create();
        layout.SetSettingsSectionSortMode(" FREQUENCY ");
        layout.SetSettingsSectionSortMode("frequency");
        layout.SetSettingsSectionExpanded("Speech", true);
        layout.SetSettingsSectionExpanded("Speech", true);
        layout.MarkSettingsSectionUsed("Speech");
        layout.SetSettingsSectionOrder((string[])["Speech", "Display"]);
        layout.SetSettingsSectionOrder((string[])["Speech", "Display"]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_settings.Layout.SettingsPage.SortMode, Is.EqualTo(SettingsSectionSortModes.Frequency));
            Assert.That(layout.SettingsSectionSortMode, Is.EqualTo(SettingsSectionSortModes.Frequency));
            Assert.That(_settings.Layout.SettingsPage.Sections["Speech"].IsExpanded, Is.True);
            Assert.That(layout.GetSettingsSectionState("Speech").UsageCount, Is.EqualTo(1));
            Assert.That(layout.GetSettingsSectionState("Speech").LastUsedUtc, Is.Not.Null);
            Assert.That(layout.GetSettingsSectionState("Display").Order, Is.EqualTo(1));
        }
        _persistence.Verify(service => service.SaveSettingsAsync(_settings), Times.Exactly(4));
    }

    [TestCase("alphabetical", SettingsSectionSortModes.Alphabetical)]
    [TestCase(" Alphabetical ", SettingsSectionSortModes.Alphabetical)]
    [TestCase("unknown", SettingsSectionSortModes.Recent)]
    [TestCase(null, SettingsSectionSortModes.Recent)]
    public void SettingsSectionSortMode_NormalizesTheSavedValue(string? saved, string expected)
    {
        _settings.Layout.SettingsPage.SortMode = saved!;

        Assert.That(Create().SettingsSectionSortMode, Is.EqualTo(expected));
    }

    [Test]
    public void SettingsSections_BlankKeysAreIgnoredOrRejected()
    {
        var layout = Create();
        layout.SetSettingsSectionExpanded(" ", true);
        layout.MarkSettingsSectionUsed(string.Empty);
        layout.SetSettingsSectionOrder((string[])[" "]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_settings.Layout.SettingsPage.Sections, Is.Empty);
            Assert.That(() => layout.GetSettingsSectionState(" "), Throws.ArgumentException);
        }
        _persistence.Verify(service => service.SaveSettingsAsync(It.IsAny<AppSettings>()), Times.Never);
    }
}
