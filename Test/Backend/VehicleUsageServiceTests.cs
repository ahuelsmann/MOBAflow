// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Service;
using global::Moba.Domain;

internal sealed class VehicleUsageServiceTests
{
    private readonly VehicleUsageService _service = new();

    [Test]
    public void CalculateTotals_Should_ApplyTrackedValuesAndCorrections()
    {
        // Arrange
        var usage = new VehicleUsageData
        {
            TrackedOperatingSeconds = 3_600,
            TrackedCompletedTrips = 10,
            TrackedDistanceKilometres = 25m,
            Corrections =
            [
                new VehicleUsageCorrection
                {
                    RecordedAt = new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero),
                    OperatingSecondsDelta = 120,
                    CompletedTripsDelta = -1,
                    DistanceKilometresDelta = 0.5m,
                    Reason = "Corrected imported counter values"
                }
            ]
        };

        // Act
        var totals = _service.CalculateTotals(usage);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(totals.OperatingSeconds, Is.EqualTo(3_720));
            Assert.That(totals.CompletedTrips, Is.EqualTo(9));
            Assert.That(totals.DistanceKilometres, Is.EqualTo(25.5m));
        });
    }

    [Test]
    public void Validate_Should_RejectCorrectionWithoutReason()
    {
        // Arrange
        var usage = new VehicleUsageData
        {
            Corrections = [new VehicleUsageCorrection { OperatingSecondsDelta = 1 }]
        };

        // Act
        var errors = _service.Validate(usage);

        // Assert
        Assert.That(errors, Has.Some.Contains("requires a reason"));
    }

    [Test]
    public void RecordCorrection_Should_NotMutateHistory_WhenEffectiveTotalWouldBeNegative()
    {
        // Arrange
        var usage = new VehicleUsageData { TrackedCompletedTrips = 1 };
        var correction = new VehicleUsageCorrection
        {
            CompletedTripsDelta = -2,
            Reason = "Remove duplicated trips"
        };

        // Act
        var exception = Assert.Throws<ArgumentException>(() => _service.RecordCorrection(usage, correction));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("must not be negative"));
            Assert.That(usage.Corrections, Is.Empty);
        });
    }

    [Test]
    public void CalculateTotals_Should_PreserveUnavailableDistance()
    {
        // Arrange
        var usage = new VehicleUsageData
        {
            TrackedOperatingSeconds = 60,
            TrackedCompletedTrips = 1
        };

        // Act
        var totals = _service.CalculateTotals(usage);

        // Assert
        Assert.That(totals.DistanceKilometres, Is.Null);
    }

    [Test]
    public void Validate_Should_RejectDistanceCorrection_WhenDistanceIsUnavailable()
    {
        // Arrange
        var usage = new VehicleUsageData
        {
            Corrections =
            [
                new VehicleUsageCorrection
                {
                    DistanceKilometresDelta = 1m,
                    Reason = "Correct measured distance"
                }
            ]
        };

        // Act
        var errors = _service.Validate(usage);

        // Assert
        Assert.That(errors, Has.Some.Contains("unavailable distance"));
    }
}
