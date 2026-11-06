// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using Moba.Domain;

/// <summary>
/// The platform a station uses: the selected platform, otherwise the first one.
/// </summary>
[TestFixture]
internal sealed class StationTests
{
    [Test]
    public void ResolvePlatform_ReturnsTheSelectedPlatform()
    {
        var first = new Platform { Number = 2 };
        var selected = new Platform { Number = 5 };
        var station = new Station { Platforms = [first, selected], PlatformId = selected.Id };

        Assert.That(station.ResolvePlatform(), Is.SameAs(selected));
    }

    [Test]
    public void ResolvePlatform_FallsBackToTheFirstPlatform()
    {
        var first = new Platform { Number = 2 };
        var unselected = new Station { Platforms = [first, new Platform { Number = 5 }] };
        var unknownSelection = new Station { Platforms = [first], PlatformId = Guid.NewGuid() };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unselected.ResolvePlatform(), Is.SameAs(first));
            Assert.That(unknownSelection.ResolvePlatform(), Is.SameAs(first));
        }
    }

    [Test]
    public void ResolvePlatform_ReturnsNullWithoutPlatforms()
    {
        var station = new Station { PlatformId = Guid.NewGuid() };

        Assert.That(station.ResolvePlatform(), Is.Null);
    }
}
