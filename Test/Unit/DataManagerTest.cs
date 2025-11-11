namespace Moba.Test.Unit;

using Backend.Data;

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

        // Output für Debugging
        TestContext.Out.WriteLine($"Total cities loaded: {dataManager.Cities.Count}");
        TestContext.Out.WriteLine("\n=== List of all German cities with main stations ===\n");

        foreach (var city in dataManager.Cities)
        {
            TestContext.Out.WriteLine($"City: {city.Name}");
            foreach (var station in city.Stations)
            {
                TestContext.Out.WriteLine($"  - Station: {station.Name}, Track: {station.Track}, Exit on Left: {station.IsExitOnLeft}");
            }
            TestContext.Out.WriteLine("");
        }
    }

    [Test]
    public async Task LoadGermanyStations_ShouldContainMajorCities()
    {
        // Act
        var dataManager = await DataManager.LoadAsync(_testFile);

        // Assert
        Assert.That(dataManager, Is.Not.Null);
        Assert.That(dataManager!.Cities, Is.Not.Null);

        // Überprüfe, dass wichtige Städte vorhanden sind
        var cityNames = dataManager.Cities.Select(c => c.Name).ToList();

        Assert.That(cityNames, Does.Contain("Berlin"), "Berlin should be in the list");
        Assert.That(cityNames, Does.Contain("München"), "München should be in the list");
        Assert.That(cityNames, Does.Contain("Hamburg"), "Hamburg should be in the list");
        Assert.That(cityNames, Does.Contain("Köln"), "Köln should be in the list");
        Assert.That(cityNames, Does.Contain("Frankfurt am Main"), "Frankfurt am Main should be in the list");
        Assert.That(cityNames, Does.Contain("Stuttgart"), "Stuttgart should be in the list");
        Assert.That(cityNames, Does.Contain("Düsseldorf"), "Düsseldorf should be in the list");
        Assert.That(cityNames, Does.Contain("Dortmund"), "Dortmund should be in the list");
        Assert.That(cityNames, Does.Contain("Leipzig"), "Leipzig should be in the list");
        Assert.That(cityNames, Does.Contain("Dresden"), "Dresden should be in the list");

        TestContext.Out.WriteLine($"✓ All major cities are present in the dataset");
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

            // Überprüfe, dass jede Station einen Namen hat
            foreach (var station in city.Stations)
            {
                Assert.That(station.Name, Is.Not.Null.And.Not.Empty,
                    $"Station in {city.Name} should have a name");
            }
        }

        TestContext.Out.WriteLine($"✓ All {dataManager.Cities.Count} cities have valid station data");
    }

    [Test]
    public async Task LoadGermanyStations_VerifySpecificStationData()
    {
        // Act
        var dataManager = await DataManager.LoadAsync(_testFile);

        // Assert
        Assert.That(dataManager, Is.Not.Null);

        // Finde München
        var munich = dataManager!.Cities.FirstOrDefault(c => c.Name == "München");
        Assert.That(munich, Is.Not.Null, "München should exist");
        Assert.That(munich!.Stations[0].Name, Is.EqualTo("München Hauptbahnhof"));

        // Finde Hamburg
        var hamburg = dataManager.Cities.FirstOrDefault(c => c.Name == "Hamburg");
        Assert.That(hamburg, Is.Not.Null, "Hamburg should exist");
        Assert.That(hamburg!.Stations[0].Name, Is.EqualTo("Hamburg Hauptbahnhof"));

        TestContext.Out.WriteLine("✓ Specific station names are correct");
        TestContext.Out.WriteLine($"München: {munich.Stations[0].Name}");
        TestContext.Out.WriteLine($"Hamburg: {hamburg.Stations[0].Name}");
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

            // Überprüfe erste Stadt
            Assert.That(reloadedData.Cities[0].Name, Is.EqualTo(originalData.Cities[0].Name));
            Assert.That(reloadedData.Cities[0].Stations[0].Name,
                Is.EqualTo(originalData.Cities[0].Stations[0].Name));

            TestContext.Out.WriteLine("✓ Save and load round-trip successful");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}