// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
#if WINDOWS

namespace Moba.Test.MOBAflow;

using Moba.WinUI.ViewModel;

/// <summary>
/// The help topic catalogue lists every section once with its own text, and the page state shows the chosen section.
/// </summary>
[TestFixture]
internal sealed class HelpPageViewModelTests
{
    private static readonly string[] RequiredNewTopics =
    [
        "Timetable",
        "Event Manager",
        "Journey Map",
        "ESP32 Display",
        "Matrix Images",
        "Monitor",
        "Recorder",
        "Settings",
        "Feature Toggles",
        "REST API",
        "MOBAsmart",
    ];

    [Test]
    public void Catalogue_GivesEverySectionItsOwnText()
    {
        var sections = HelpTopics.Categories.SelectMany(category => category.Sections).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sections, Is.Unique);
            Assert.That(sections, Does.Contain(HelpTopics.DefaultSection));
            Assert.That(sections.Select(HelpTopics.GetContent), Has.None.EqualTo(HelpTopics.MissingSectionText));
            Assert.That(HelpTopics.GetContent("Unknown topic"), Is.EqualTo(HelpTopics.MissingSectionText));
        }
    }

    [Test]
    public void ShowSection_SelectsTheSectionAndReportsItsText()
    {
        var viewModel = new HelpPageViewModel();
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        viewModel.ShowSection("No Connection");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.SelectedSection, Is.EqualTo("No Connection"));
            Assert.That(viewModel.SelectedContent, Does.StartWith("Problem: Cannot connect to Z21"));
            Assert.That(changed, Does.Contain(nameof(HelpPageViewModel.SelectedContent)));
            Assert.Throws<ArgumentException>(() => viewModel.ShowSection(" "));
        }
    }

    [Test]
    public void Catalogue_IncludesAllExpectedTopics()
    {
        var sections = HelpTopics.Categories.SelectMany(category => category.Sections).ToList();

        using (Assert.EnterMultipleScope())
        {
            foreach (var topic in RequiredNewTopics)
            {
                Assert.That(sections, Does.Contain(topic), $"Missing help topic category entry: {topic}");
                Assert.That(HelpTopics.GetContent(topic), Is.Not.EqualTo(HelpTopics.MissingSectionText), $"Missing help topic content: {topic}");
            }
        }
    }

    [Test]
    public void Content_ReflectsCurrentDesktopBehaviorWithoutDrift()
    {
        using (Assert.EnterMultipleScope())
        {
            var emergencyStop = HelpTopics.GetContent("Emergency Stop");
            Assert.That(emergencyStop, Does.Contain("Pause"));
            Assert.That(emergencyStop, Does.Contain("Space"));
            Assert.That(emergencyStop, Does.Not.Contain("F12"));
            Assert.That(emergencyStop, Does.Not.Contain("Escape"));

            var speedControl = HelpTopics.GetContent("Speed Control");
            Assert.That(speedControl, Does.Contain("14 steps"));
            Assert.That(speedControl, Does.Contain("28 steps"));
            Assert.That(speedControl, Does.Contain("128 steps"));
            Assert.That(speedControl, Does.Not.Contain("126 steps (recommended)"));

            var workflows = HelpTopics.GetContent("Workflows");
            Assert.That(workflows, Does.Contain("Delay after"));
            Assert.That(workflows, Does.Not.Contain("Parallel"));

            var trackPlanEditor = HelpTopics.GetContent("Track Plan Editor");
            Assert.That(trackPlanEditor, Does.Contain("The operator is responsible for safe operation."));
            Assert.That(trackPlanEditor, Does.Not.Contain("AnyRail"));

            var recorder = HelpTopics.GetContent("Recorder");
            Assert.That(recorder, Does.Contain(".mobarecording.json"));
            Assert.That(recorder, Does.Contain("in-memory"));
            Assert.That(recorder, Does.Contain("never sends commands to hardware").IgnoreCase);

            var featureToggles = HelpTopics.GetContent("Feature Toggles");
            Assert.That(featureToggles, Does.Contain("Settings"));
            Assert.That(featureToggles, Does.Contain("Overview"));
        }
    }
}
#endif
