// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
#if WINDOWS
namespace Moba.Test.WinUI;

using Moba.WinUI.Service;

/// <summary>
/// The navigation item name is what Narrator speaks; WinUI items cannot be created without a XAML runtime,
/// so these tests cover the name composition used by <see cref="NavigationItemFactory"/>.
/// </summary>
[TestFixture]
internal sealed class NavigationItemFactoryTests
{
    [Test]
    public void ComposeAccessibleName_WithBadge_AppendsBadgeAfterTitle()
    {
        Assert.That(NavigationItemFactory.ComposeAccessibleName("Timetable", "Preview"), Is.EqualTo("Timetable, Preview"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void ComposeAccessibleName_WithoutBadge_IsTitle(string? badgeLabel)
    {
        Assert.That(NavigationItemFactory.ComposeAccessibleName("Train Control", badgeLabel), Is.EqualTo("Train Control"));
    }
}
#endif
