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
    public void EmergencyStop_DocumentsPauseAndSpaceForSelectedLocomotiveOnly()
    {
        var text = HelpTopics.GetContent("Emergency Stop");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Does.Contain("Pause"));
            Assert.That(text, Does.Contain("Space"));
            Assert.That(text, Does.Contain("selected locomotive").IgnoreCase);
            Assert.That(text, Does.Contain("speed step 0").IgnoreCase.Or.Contains("speed 0").IgnoreCase);
            Assert.That(text, Does.Not.Contain("Escape"));
            Assert.That(text, Does.Not.Contain("F12"));
            Assert.That(text, Does.Not.Contain("all locomotives").IgnoreCase);
            Assert.That(text, Does.Not.Contain("Track power is cut off").IgnoreCase);
        }
    }

    [Test]
    public void Direction_DoesNotClaimSpacebarChangesDirection()
    {
        var text = HelpTopics.GetContent("Direction");

        Assert.That(text, Does.Not.Contain("spacebar").IgnoreCase);
    }

    [Test]
    public void SpeedControl_Documents14_28And128SpeedSteps()
    {
        var text = HelpTopics.GetContent("Speed Control");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Does.Contain("14 steps").IgnoreCase);
            Assert.That(text, Does.Contain("28 steps").IgnoreCase);
            Assert.That(text, Does.Contain("128 steps").IgnoreCase);
            Assert.That(text, Does.Not.Contain("126 steps").IgnoreCase);
        }
    }

    [Test]
    public void Z21Connection_DocumentsPerProjectSetupOnSolutionPage()
    {
        var text = HelpTopics.GetContent("Z21 Connection");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Does.Contain("Solution"));
            Assert.That(text, Does.Contain("Z21 Finder"));
            Assert.That(text, Does.Contain("per project").IgnoreCase.Or.Contains("each project").IgnoreCase);
            Assert.That(text, Does.Not.Contain("Click \"Connect\""));
        }
    }

    [Test]
    public void Installation_DoesNotClaimAutoDotNetInstallOrStartupUpdateCheck()
    {
        var text = HelpTopics.GetContent("Installation");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Does.Not.Contain("automatically installed"));
            Assert.That(text, Does.Not.Contain("checks for updates"));
        }
    }

    [Test]
    public void Workflows_DocumentsActionListWithDelayAfterWithoutParallelOrLoopModes()
    {
        var text = HelpTopics.GetContent("Workflows");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Does.Contain("Delay after"));
            Assert.That(text, Does.Not.Contain("Parallel"));
            Assert.That(text, Does.Not.Contain("Loop"));
            Assert.That(text, Does.Not.Contain("Execution modes"));
        }
    }

    [Test]
    public void DefineStations_DocumentsPlatformsInPortAndEventManager()
    {
        var text = HelpTopics.GetContent("Define Stations");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Does.Contain("platforms").IgnoreCase);
            Assert.That(text, Does.Contain("InPort"));
            Assert.That(text, Does.Contain("Event Manager"));
            Assert.That(text, Does.Not.Contain("Departure left/right"));
            Assert.That(text, Does.Not.Contain("Feedback point ID"));
        }
    }

    [Test]
    public void TrackPlanEditor_UsesTrackPlanTitleAndActualShortcutsAndKeepsSafetyNotice()
    {
        var text = HelpTopics.GetContent("Track Plan Editor");
        var placeTracksText = HelpTopics.GetContent("Place Tracks");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Does.Not.Contain("Signal Box - Direct Layout Operation"));
            Assert.That(text, Does.Contain("Track Plan Editor"));
            Assert.That(text, Does.Contain("Ctrl+Z"));
            Assert.That(text, Does.Contain("Ctrl+Y"));
            Assert.That(text, Does.Contain("Ctrl+0"));
            Assert.That(text, Does.Contain("Ctrl+1"));
            Assert.That(text, Does.Contain("Delete"));
            Assert.That(text, Does.Contain("MOBAflow does not reserve routes"));
            Assert.That(text, Does.Contain("operator is responsible for safe operation"));
            Assert.That(placeTracksText, Does.Not.Contain("Rotate with R"));
            Assert.That(placeTracksText, Does.Not.Contain("Ctrl+C"));
        }
    }

    [Test]
    public void HelpPageXaml_UsesEnglishUiLabelsWithoutGermanPlaceholders()
    {
        var repoRoot = FindRepoRoot();
        var helpPageXamlPath = Path.Combine(repoRoot, "MOBAflow", "View", "HelpPage.xaml");
        var xaml = File.ReadAllText(helpPageXamlPath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(xaml, Does.Not.Contain("Dokumentation"));
            Assert.That(xaml, Does.Not.Contain("Willkommen"));
            Assert.That(xaml, Does.Contain("Documentation"));
            Assert.That(xaml, Does.Contain("Welcome"));
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Moba.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Moba.slnx was not found above the test directory.");
    }
}
#endif
