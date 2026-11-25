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
}
#endif
