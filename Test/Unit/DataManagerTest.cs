// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace Moba.Test.Unit;

using Moba.Backend.Data;

public class DataManagerTest
{
    private string _testFile = string.Empty;

    [SetUp]
    public void Setup()
    {
        _testFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestFile\germany-stations.json");

        Assert.That(File.Exists(_testFile), Is.True,
            $"Test file germany-stations.json not found: {_testFile}");
    }

    [Test]
    public async Task LoadGermanyStations_ShouldLoadSuccessfully()
    {
        // Act
        var dataManager = await DataManager.LoadAsync(_testFile);

        // Assert
        Assert.That(dataManager, Is.Not.Null, "DataManager should not be null");
        Assert.That(dataManager!.Cities, Is.Not.Null, "Cities list should not be null");
        Assert.That(dataManager.Cities.Count, Is.GreaterThan(0), "Cities list should contain entries");
    }

    [Test]
    public async Task LoadGermanyStations_ShouldContainSampleCities()
    {
        // Act
        var dataManager = await DataManager.LoadAsync(_testFile);

        // Assert
        Assert.That(dataManager, Is.Not.Null);
        Assert.That(dataManager!.Cities, Is.Not.Null);

        var cityNames = dataManager.Cities.Select(c => c.Name).ToList();

        // Validate a subset present in the minimal dataset
        Assert.That(cityNames, Does.Contain("Berlin"));
        Assert.That(cityNames, Does.Contain("München"));
        Assert.That(cityNames, Does.Contain("Hamburg"));
    }

    [Test]
    public async Task LoadGermanyStations_EachCityShouldHaveAtLeastOneStation()
    {
        // Act
        var dataManager = await DataManager.LoadAsync(_testFile);

        // Assert
        Assert.That(dataManager, Is.Not.Null);
        Assert.That(dataManager!.Cities, Is.Not.Null);

        foreach (var city in dataManager.Cities)
        {
            Assert.That(city.Stations, Is.Not.Null, $"{city.Name} should have a stations list");
            Assert.That(city.Stations.Count, Is.GreaterThanOrEqualTo(1),
                $"{city.Name} should have at least one station");

            foreach (var station in city.Stations)
            {
                Assert.That(station.Name, Is.Not.Null.And.Not.Empty,
                    $"Station in {city.Name} should have a name");
            }
        }
    }

    [Test]
    public async Task LoadGermanyStations_VerifySpecificStationData_Sample()
    {
        // Act
        var dataManager = await DataManager.LoadAsync(_testFile);

        // Assert
        Assert.That(dataManager, Is.Not.Null);

        var munich = dataManager!.Cities.FirstOrDefault(c => c.Name == "München");
        Assert.That(munich, Is.Not.Null, "München should exist");
        Assert.That(munich!.Stations[0].Name, Is.EqualTo("München Hauptbahnhof"));

        var hamburg = dataManager.Cities.FirstOrDefault(c => c.Name == "Hamburg");
        Assert.That(hamburg, Is.Not.Null, "Hamburg should exist");
        Assert.That(hamburg!.Stations[0].Name, Is.EqualTo("Hamburg Hauptbahnhof"));
    }

    [Test]
    public async Task SaveAndLoadGermanyStations_ShouldRoundTrip()
    {
        // Arrange
        var originalData = await DataManager.LoadAsync(_testFile);
        var tempFile = Path.Combine(Path.GetTempPath(), "test-stations.json");

        try
        {
            // Act - Save
            await DataManager.SaveAsync(tempFile, originalData);
            Assert.That(File.Exists(tempFile), Is.True, "Temp file should be created");

            // Act - Load
            var reloadedData = await DataManager.LoadAsync(tempFile);

            // Assert
            Assert.That(reloadedData, Is.Not.Null);
            Assert.That(originalData, Is.Not.Null);
            Assert.That(reloadedData!.Cities.Count, Is.EqualTo(originalData!.Cities.Count),
                "Number of cities should match");

            // Verify first city exists and has at least one station
            Assert.That(reloadedData.Cities[0].Stations.Count, Is.GreaterThanOrEqualTo(1));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}