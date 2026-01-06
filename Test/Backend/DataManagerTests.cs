// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Data;

/// <summary>
/// Unit tests for DataManager JSON deserialization.
/// Tests file I/O operations for city library data loading.
/// </summary>
[TestFixture]
public class DataManagerTests
{
    private string _tempDir = null!;
    private string _testFilePath = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"DataManagerTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _testFilePath = Path.Combine(_tempDir, "test-data.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Test]
    public void Constructor_ShouldInitializeEmptyCities()
    {
        // Act
        var manager = new DataManager();

        // Assert
        Assert.That(manager.Cities, Is.Not.Null, "Cities should be initialized");
        Assert.That(manager.Cities, Is.Empty, "Cities should be empty initially");
    }

    [Test]
    public async Task LoadAsync_WithNonExistentFile_ShouldReturnNull()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDir, "non-existent.json");

        // Act
        var result = await DataManager.LoadAsync(nonExistentPath);

        // Assert
        Assert.That(result, Is.Null, "Should return null for non-existent file");
    }

    [Test]
    public async Task LoadAsync_WithEmptyFile_ShouldReturnNull()
    {
        // Arrange
        await File.WriteAllTextAsync(_testFilePath, "");

        // Act
        var result = await DataManager.LoadAsync(_testFilePath);

        // Assert
        Assert.That(result, Is.Null, "Should return null for empty file");
    }

    [Test]
    public async Task LoadAsync_WithEmptyJsonObject_ShouldReturnEmptyDataManager()
    {
        // Arrange
        var jsonContent = @"{ ""Cities"": [] }";
        await File.WriteAllTextAsync(_testFilePath, jsonContent);

        // Act
        var result = await DataManager.LoadAsync(_testFilePath);

        // Assert
        Assert.That(result, Is.Not.Null, "Should return DataManager instance");
        Assert.That(result!.Cities, Is.Empty, "Cities should be empty");
    }

    [Test]
    public async Task LoadAsync_WithValidJsonFile_ShouldDeserializeData()
    {
        // Arrange
        var jsonContent = @"
        {
            ""Cities"": [
                { ""Name"": ""Berlin"", ""Latitude"": 52.52, ""Longitude"": 13.405 },
                { ""Name"": ""Munich"", ""Latitude"": 48.137, ""Longitude"": 11.576 }
            ]
        }";
        await File.WriteAllTextAsync(_testFilePath, jsonContent);

        // Act
        var result = await DataManager.LoadAsync(_testFilePath);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Cities.Count, Is.EqualTo(2), "Should load 2 cities");
        Assert.That(result.Cities[0].Name, Is.EqualTo("Berlin"), "First city should be Berlin");
        Assert.That(result.Cities[1].Name, Is.EqualTo("Munich"), "Second city should be Munich");
    }

    [Test]
    public async Task LoadAsync_WithInvalidJson_ShouldReturnNull()
    {
        // Arrange
        await File.WriteAllTextAsync(_testFilePath, "{ invalid json content }");

        // Act
        var result = await DataManager.LoadAsync(_testFilePath);

        // Assert
        Assert.That(result, Is.Null, "Should return null for invalid JSON");
    }

    [Test]
    public async Task LoadAsync_WithNullPath_ShouldReturnNull()
    {
        // Act
        var result = await DataManager.LoadAsync(null!);

        // Assert
        Assert.That(result, Is.Null, "Should return null for null path");
    }

    [Test]
    public async Task LoadAsync_WithEmptyPath_ShouldReturnNull()
    {
        // Act
        var result = await DataManager.LoadAsync("");

        // Assert
        Assert.That(result, Is.Null, "Should return null for empty path");
    }

    [Test]
    public async Task LoadAsync_WithMultipleCities_ShouldLoadAll()
    {
        // Arrange
        var jsonContent = @"
        {
            ""Cities"": [
                { ""Name"": ""Berlin"" },
                { ""Name"": ""Munich"" },
                { ""Name"": ""Hamburg"" },
                { ""Name"": ""Frankfurt"" },
                { ""Name"": ""Cologne"" }
            ]
        }";
        await File.WriteAllTextAsync(_testFilePath, jsonContent);

        // Act
        var result = await DataManager.LoadAsync(_testFilePath);

        // Assert
        Assert.That(result!.Cities.Count, Is.EqualTo(5), "Should load all 5 cities");
        var cityNames = result.Cities.Select(c => c.Name).ToList();
        Assert.That(cityNames, Contains.Item("Berlin"));
        Assert.That(cityNames, Contains.Item("Munich"));
        Assert.That(cityNames, Contains.Item("Hamburg"));
        Assert.That(cityNames, Contains.Item("Frankfurt"));
        Assert.That(cityNames, Contains.Item("Cologne"));
    }

    [Test]
    public async Task LoadAsync_WithWhitespaceOnlyFile_ShouldReturnNull()
    {
        // Arrange
        await File.WriteAllTextAsync(_testFilePath, "   \n  \t  \n  ");

        // Act
        var result = await DataManager.LoadAsync(_testFilePath);

        // Assert
        Assert.That(result, Is.Null, "Should return null for whitespace-only file");
    }
}
