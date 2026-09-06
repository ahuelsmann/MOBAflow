// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Domain;

using global::Moba.Domain;

using System.Text.Json;

internal sealed class VehicleMaintenanceSerializationTests
{
    [Test]
    public void MaintenanceData_Should_RoundTripForLocomotivesAndWagons()
    {
        // Arrange
        var planId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var maintenance = new VehicleMaintenanceData
        {
            Plans =
            [
                new VehicleMaintenancePlan
                {
                    Id = planId,
                    Name = "Coupler inspection",
                    Category = MaintenanceCategory.Other,
                    IntervalCompletedTrips = 12,
                    CompletedTripsAtLastCompletion = 3
                }
            ]
        };
        var locomotive = new Locomotive { Name = "BR 218", Maintenance = maintenance };
        var wagon = new GoodsWagon { Name = "Boxcar", Maintenance = maintenance };

        // Act
        var restoredLocomotive = RoundTrip(locomotive);
        var restoredWagon = RoundTrip(wagon);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(restoredLocomotive.Maintenance!.Plans.Single().Id, Is.EqualTo(planId));
            Assert.That(restoredLocomotive.Maintenance.Plans.Single().IntervalCompletedTrips, Is.EqualTo(12));
            Assert.That(restoredWagon.Maintenance!.Plans.Single().Name, Is.EqualTo("Coupler inspection"));
            Assert.That(restoredWagon.Maintenance.Plans.Single().CompletedTripsAtLastCompletion, Is.EqualTo(3));
        });
    }

    [Test]
    public void RollingStockJson_Should_RemainReadableWithoutMaintenanceData()
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
            Assert.That(locomotive!.Maintenance, Is.Null);
            Assert.That(wagon!.Maintenance, Is.Null);
        });
    }

    private static T RoundTrip<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions.Default);
        return JsonSerializer.Deserialize<T>(json, JsonOptions.Default)!;
    }
}
