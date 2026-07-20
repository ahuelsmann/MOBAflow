// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Domain;

using global::Moba.Domain;

using System.Text.Json;

internal sealed class VehicleUsageSerializationTests
{
    [Test]
    public void RollingStockJson_Should_RemainReadableWithoutUsageData()
    {
        // Arrange
        const string locomotiveJson = """{"id":"11111111-1111-1111-1111-111111111111","name":"BR 218"}""";
        const string wagonJson = """{"id":"22222222-2222-2222-2222-222222222222","name":"Coach"}""";

        // Act
        var locomotive = JsonSerializer.Deserialize<Locomotive>(locomotiveJson, JsonOptions.Default);
        var wagon = JsonSerializer.Deserialize<PassengerWagon>(wagonJson, JsonOptions.Default);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(locomotive!.Usage, Is.Null);
            Assert.That(wagon!.Usage, Is.Null);
        });
    }

    [Test]
    public void UsageData_Should_RoundTripForLocomotivesAndWagons()
    {
        // Arrange
        var correctionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var usage = new VehicleUsageData
        {
            TrackedOperatingSeconds = 7_200,
            TrackedCompletedTrips = 4,
            TrackedDistanceKilometres = 12.5m,
            Corrections =
            [
                new VehicleUsageCorrection
                {
                    Id = correctionId,
                    RecordedAt = new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero),
                    OperatingSecondsDelta = -60,
                    Reason = "Remove test run"
                }
            ]
        };
        var locomotive = new Locomotive { Name = "BR 218", Usage = usage };
        var wagon = new GoodsWagon { Name = "Boxcar", Usage = usage };

        // Act
        var restoredLocomotive = RoundTrip(locomotive);
        var restoredWagon = RoundTrip(wagon);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(restoredLocomotive.Usage!.TrackedOperatingSeconds, Is.EqualTo(7_200));
            Assert.That(restoredLocomotive.Usage.Corrections.Single().Id, Is.EqualTo(correctionId));
            Assert.That(restoredLocomotive.Usage.Corrections.Single().Reason, Is.EqualTo("Remove test run"));
            Assert.That(restoredWagon.Usage!.TrackedCompletedTrips, Is.EqualTo(4));
            Assert.That(restoredWagon.Usage.TrackedDistanceKilometres, Is.EqualTo(12.5m));
        });
    }

    private static T RoundTrip<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions.Default);
        return JsonSerializer.Deserialize<T>(json, JsonOptions.Default)!;
    }
}
