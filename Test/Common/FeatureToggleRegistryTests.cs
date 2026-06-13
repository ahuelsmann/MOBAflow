// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Configuration;

/// <summary>
/// Tests for <see cref="FeatureToggleRegistry"/>, the reflection-free accessor registry
/// for navigation page toggles and badge labels. These guard the contract that every
/// registered key round-trips through the get/set accessors and that unknown keys are
/// handled defensively.
/// </summary>
[TestFixture]
internal sealed class FeatureToggleRegistryTests
{
    [Test]
    public void PageAvailabilityKeys_CoverAllRegisteredAccessors()
    {
        var settings = new FeatureToggleSettings();

        // Every advertised key must be readable through the typed accessor.
        Assert.That(FeatureToggleRegistry.PageAvailabilityKeys, Is.Not.Empty);
        foreach (var key in FeatureToggleRegistry.PageAvailabilityKeys)
        {
            Assert.That(FeatureToggleRegistry.TryGetPageAvailability(settings, key, out _), Is.True,
                $"Key '{key}' is advertised but has no accessor.");
        }
    }

    [Test]
    public void TryGetPageAvailability_KnownKey_ReturnsCurrentValue()
    {
        var settings = new FeatureToggleSettings { IsMonitorPageAvailable = false };

        var found = FeatureToggleRegistry.TryGetPageAvailability(
            settings, nameof(FeatureToggleSettings.IsMonitorPageAvailable), out var value);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(value, Is.False);
        });
    }

    [Test]
    public void TryGetPageAvailability_UnknownKey_ReturnsFalseAndDefaultValue()
    {
        var settings = new FeatureToggleSettings();

        var found = FeatureToggleRegistry.TryGetPageAvailability(settings, "DoesNotExist", out var value);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.False);
            Assert.That(value, Is.False);
        });
    }

    [Test]
    public void TrySetPageAvailability_KnownKey_UpdatesSettings()
    {
        var settings = new FeatureToggleSettings { IsJourneysPageAvailable = true };

        var changed = FeatureToggleRegistry.TrySetPageAvailability(
            settings, nameof(FeatureToggleSettings.IsJourneysPageAvailable), value: false);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(settings.IsJourneysPageAvailable, Is.False);
        });
    }

    [Test]
    public void TrySetPageAvailability_UnknownKey_ReturnsFalseAndLeavesSettingsUntouched()
    {
        var settings = new FeatureToggleSettings { IsJourneysPageAvailable = true };

        var changed = FeatureToggleRegistry.TrySetPageAvailability(settings, "Nope", value: false);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.False);
            Assert.That(settings.IsJourneysPageAvailable, Is.True);
        });
    }

    [Test]
    public void SetThenGet_RoundTripsForEveryRegisteredKey()
    {
        var settings = new FeatureToggleSettings();

        foreach (var key in FeatureToggleRegistry.PageAvailabilityKeys)
        {
            FeatureToggleRegistry.TrySetPageAvailability(settings, key, value: false);
            FeatureToggleRegistry.TryGetPageAvailability(settings, key, out var afterFalse);

            FeatureToggleRegistry.TrySetPageAvailability(settings, key, value: true);
            FeatureToggleRegistry.TryGetPageAvailability(settings, key, out var afterTrue);

            Assert.Multiple(() =>
            {
                Assert.That(afterFalse, Is.False, $"Key '{key}' did not store false.");
                Assert.That(afterTrue, Is.True, $"Key '{key}' did not store true.");
            });
        }
    }

    [Test]
    public void GetPageAvailabilityOrDefault_UnknownKey_ReturnsProvidedDefault()
    {
        var settings = new FeatureToggleSettings();

        Assert.Multiple(() =>
        {
            Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(settings, "Unknown"), Is.True);
            Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(settings, "Unknown", defaultIfUnknown: false), Is.False);
        });
    }

    [Test]
    public void GetPageAvailabilityOrDefault_KnownKey_IgnoresDefault()
    {
        var settings = new FeatureToggleSettings { IsDisplayPageAvailable = false };

        var value = FeatureToggleRegistry.GetPageAvailabilityOrDefault(
            settings, nameof(FeatureToggleSettings.IsDisplayPageAvailable), defaultIfUnknown: true);

        Assert.That(value, Is.False);
    }

    [Test]
    public void GetBadgeLabel_KnownNonEmptyLabel_ReturnsValue()
    {
        var settings = new FeatureToggleSettings { MonitorPageLabel = "Preview" };

        var label = FeatureToggleRegistry.GetBadgeLabel(
            settings, nameof(FeatureToggleSettings.MonitorPageLabel));

        Assert.That(label, Is.EqualTo("Preview"));
    }

    [Test]
    public void GetBadgeLabel_KnownButWhitespaceLabel_ReturnsNull()
    {
        var settings = new FeatureToggleSettings { OverviewPageLabel = "   " };

        var label = FeatureToggleRegistry.GetBadgeLabel(
            settings, nameof(FeatureToggleSettings.OverviewPageLabel));

        Assert.That(label, Is.Null);
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("UnknownProperty")]
    public void GetBadgeLabel_MissingOrUnknownName_ReturnsNull(string? name)
    {
        var settings = new FeatureToggleSettings { MonitorPageLabel = "Preview" };

        Assert.That(FeatureToggleRegistry.GetBadgeLabel(settings, name), Is.Null);
    }
}
