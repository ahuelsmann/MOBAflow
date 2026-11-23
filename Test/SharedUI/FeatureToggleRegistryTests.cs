// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Domain;
using Moba.SharedUI.Configuration;
using System.Text.Json;

/// <summary>
/// Tests for <see cref="FeatureToggleRegistry"/>, the registry of keyed page toggles and badge labels.
/// They guard registry defaults for missing keys, that every registered key round-trips through the
/// get/set accessors and that unknown keys are handled defensively.
/// </summary>
[TestFixture]
internal sealed class FeatureToggleRegistryTests
{
    [Test]
    public void PageKeys_AreAvailableByDefault()
    {
        var settings = new FeatureToggleSettings();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FeatureToggleRegistry.PageKeys, Has.Count.EqualTo(19));
            foreach (var key in FeatureToggleRegistry.PageKeys)
            {
                Assert.That(FeatureToggleRegistry.TryGetPageAvailability(settings, key, out var available), Is.True,
                    $"Key '{key}' is advertised but cannot be read.");
                Assert.That(available, Is.True, $"Key '{key}' is not available by default.");
            }
        }
    }

    [Test]
    public void TryGetPageAvailability_KnownKey_ReturnsStoredValue()
    {
        var settings = new FeatureToggleSettings();
        settings.Pages[FeatureTogglePages.Monitor] = new FeatureTogglePageSettings { IsAvailable = false };

        var found = FeatureToggleRegistry.TryGetPageAvailability(settings, FeatureTogglePages.Monitor, out var value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.True);
            Assert.That(value, Is.False);
        }
    }

    [Test]
    public void TryGetPageAvailability_UnknownKey_ReturnsFalseAndDefaultValue()
    {
        var settings = new FeatureToggleSettings();
        settings.Pages["DoesNotExist"] = new FeatureTogglePageSettings { IsAvailable = true };

        var found = FeatureToggleRegistry.TryGetPageAvailability(settings, "DoesNotExist", out var value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.False);
            Assert.That(value, Is.False);
        }
    }

    [Test]
    public void TrySetPageAvailability_MissingKey_AddsEntryWithDefaultLabel()
    {
        var settings = new FeatureToggleSettings();

        var changed = FeatureToggleRegistry.TrySetPageAvailability(settings, FeatureTogglePages.Journeys, value: false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(changed, Is.True);
            Assert.That(settings.Pages[FeatureTogglePages.Journeys].IsAvailable, Is.False);
            Assert.That(settings.Pages[FeatureTogglePages.Journeys].Label, Is.Null);
        }
    }

    [Test]
    public void TrySetPageAvailability_ExistingKey_KeepsStoredLabel()
    {
        var settings = new FeatureToggleSettings();
        settings.Pages[FeatureTogglePages.Trains] = new FeatureTogglePageSettings { Label = "Beta" };

        FeatureToggleRegistry.TrySetPageAvailability(settings, FeatureTogglePages.Trains, value: false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.Pages[FeatureTogglePages.Trains].IsAvailable, Is.False);
            Assert.That(FeatureToggleRegistry.GetBadgeLabel(settings, FeatureTogglePages.Trains), Is.EqualTo("Beta"));
        }
    }

    [Test]
    public void TrySetPageAvailability_UnknownKey_ReturnsFalseAndLeavesSettingsUntouched()
    {
        var settings = new FeatureToggleSettings();

        var changed = FeatureToggleRegistry.TrySetPageAvailability(settings, "Nope", value: false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(changed, Is.False);
            Assert.That(settings.Pages, Is.Empty);
        }
    }

    [Test]
    public void SetThenGet_RoundTripsForEveryRegisteredKey()
    {
        var settings = new FeatureToggleSettings();

        foreach (var key in FeatureToggleRegistry.ToggleablePageKeys)
        {
            FeatureToggleRegistry.TrySetPageAvailability(settings, key, value: false);
            FeatureToggleRegistry.TryGetPageAvailability(settings, key, out var afterFalse);

            FeatureToggleRegistry.TrySetPageAvailability(settings, key, value: true);
            FeatureToggleRegistry.TryGetPageAvailability(settings, key, out var afterTrue);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterFalse, Is.False, $"Key '{key}' did not store false.");
                Assert.That(afterTrue, Is.True, $"Key '{key}' did not store true.");
            }
        }
    }

    [Test]
    public void OverviewIsAlwaysAvailableAndCannotBeDisabled()
    {
        var settings = new FeatureToggleSettings();

        var disabled = FeatureToggleRegistry.TrySetPageAvailability(settings, FeatureTogglePages.Overview, value: false);
        var found = FeatureToggleRegistry.TryGetPageAvailability(settings, FeatureTogglePages.Overview, out var available);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FeatureToggleRegistry.IsRegistered(FeatureTogglePages.Overview), Is.True);
            Assert.That(FeatureToggleRegistry.IsAlwaysAvailable(FeatureTogglePages.Overview), Is.True);
            Assert.That(FeatureToggleRegistry.IsToggleable(FeatureTogglePages.Overview), Is.False);
            Assert.That(FeatureToggleRegistry.ToggleablePageKeys, Has.Count.EqualTo(18));
            Assert.That(FeatureToggleRegistry.ToggleablePageKeys, Does.Not.Contain(FeatureTogglePages.Overview));
            Assert.That(disabled, Is.False);
            Assert.That(found, Is.True);
            Assert.That(available, Is.True);
            Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(settings, FeatureTogglePages.Overview, defaultIfUnknown: false), Is.True);
        }
    }

    [Test]
    public void NormalizeReEnablesLegacyDisabledOverviewWithoutAffectingOtherToggles()
    {
        const string json = """
            {"pages":{"Overview":{"isAvailable":false,"label":"Home"},"Monitor":{"isAvailable":false}}}
            """;

        var settings = JsonSerializer.Deserialize<FeatureToggleSettings>(json, JsonOptions.Default)!;
        var beforeNormalizeAvailable = FeatureToggleRegistry.GetPageAvailabilityOrDefault(settings, FeatureTogglePages.Overview);

        FeatureToggleRegistry.Normalize(settings);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(beforeNormalizeAvailable, Is.True);
            Assert.That(settings.Pages[FeatureTogglePages.Overview].IsAvailable, Is.True);
            Assert.That(FeatureToggleRegistry.GetBadgeLabel(settings, FeatureTogglePages.Overview), Is.EqualTo("Home"));
            Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(settings, FeatureTogglePages.Monitor), Is.False);
        }
    }

    [Test]
    public void GetPageAvailabilityOrDefault_UnknownKey_ReturnsProvidedDefault()
    {
        var settings = new FeatureToggleSettings();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(settings, "Unknown"), Is.True);
            Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(settings, "Unknown", defaultIfUnknown: false), Is.False);
        }
    }

    [Test]
    public void GetPageAvailabilityOrDefault_KnownKey_IgnoresDefault()
    {
        var settings = new FeatureToggleSettings();
        settings.Pages[FeatureTogglePages.Display] = new FeatureTogglePageSettings { IsAvailable = false };

        var value = FeatureToggleRegistry.GetPageAvailabilityOrDefault(settings, FeatureTogglePages.Display, defaultIfUnknown: true);

        Assert.That(value, Is.False);
    }

    [TestCase(FeatureTogglePages.Timetable, "Preview")]
    [TestCase(FeatureTogglePages.TrainControl, "Preview")]
    [TestCase(FeatureTogglePages.Overview, null)]
    [TestCase(FeatureTogglePages.Locomotives, null)]
    public void GetBadgeLabel_MissingKey_ReturnsRegistryDefault(string key, string? expected)
    {
        Assert.That(FeatureToggleRegistry.GetBadgeLabel(new FeatureToggleSettings(), key), Is.EqualTo(expected));
    }

    [Test]
    public void GetBadgeLabel_StoredLabelOverridesDefaultAndEmptyHidesBadge()
    {
        var settings = new FeatureToggleSettings();
        settings.Pages[FeatureTogglePages.Overview] = new FeatureTogglePageSettings { Label = "Beta" };
        settings.Pages[FeatureTogglePages.Monitor] = new FeatureTogglePageSettings { Label = string.Empty };
        settings.Pages[FeatureTogglePages.Timetable] = new FeatureTogglePageSettings { IsAvailable = false };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FeatureToggleRegistry.GetBadgeLabel(settings, FeatureTogglePages.Overview), Is.EqualTo("Beta"));
            Assert.That(FeatureToggleRegistry.GetBadgeLabel(settings, FeatureTogglePages.Monitor), Is.Null);
            Assert.That(FeatureToggleRegistry.GetBadgeLabel(settings, FeatureTogglePages.Timetable), Is.EqualTo("Preview"));
        }
    }

    [Test]
    public void GetBadgeLabel_KnownButWhitespaceLabel_ReturnsNull()
    {
        var settings = new FeatureToggleSettings();
        settings.Pages[FeatureTogglePages.Overview] = new FeatureTogglePageSettings { Label = "   " };

        Assert.That(FeatureToggleRegistry.GetBadgeLabel(settings, FeatureTogglePages.Overview), Is.Null);
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("UnknownProperty")]
    public void GetBadgeLabel_MissingOrUnknownName_ReturnsNull(string? name)
    {
        Assert.That(FeatureToggleRegistry.GetBadgeLabel(new FeatureToggleSettings(), name), Is.Null);
    }

    [Test]
    public void Json_LoadsStoredPagesAndIgnoresRemovedFlatToggles()
    {
        const string json = """
            {"isMonitorPageAvailable":false,"monitorPageLabel":"Old",
             "pages":{"SignalBox":{"isAvailable":false},"Trains":{"label":"Beta"}}}
            """;

        var settings = JsonSerializer.Deserialize<FeatureToggleSettings>(json, JsonOptions.Default)!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(settings, FeatureTogglePages.SignalBox), Is.False);
            Assert.That(FeatureToggleRegistry.GetBadgeLabel(settings, FeatureTogglePages.SignalBox), Is.EqualTo("Preview"));
            Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(settings, FeatureTogglePages.Trains), Is.True);
            Assert.That(FeatureToggleRegistry.GetBadgeLabel(settings, FeatureTogglePages.Trains), Is.EqualTo("Beta"));
            Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(settings, FeatureTogglePages.Monitor), Is.True);
            Assert.That(FeatureToggleRegistry.GetBadgeLabel(settings, FeatureTogglePages.Monitor), Is.EqualTo("Preview"));
        }
    }
}
