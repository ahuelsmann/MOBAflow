// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Data;

/// <summary>
/// Unit tests for <see cref="MasterDataStore"/> JSON deserialization.
/// Tests file I/O operations for city library data loading.
/// </summary>
[TestFixture]
internal class MasterDataStoreTests
{
    private string _tempDir = null!;
    private string _testFilePath = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"MasterDataStoreTests_{Guid.NewGuid()}");
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
        var store = new MasterDataStore();

        Assert.That(store.Cities, Is.Not.Null, "Cities should be initialized");
        Assert.That(store.Cities, Is.Empty, "Cities should be empty initially");
    }

    [Test]
    public async Task LoadAsync_WithNonExistentFile_ShouldReturnNull()
    {
        var nonExistentPath = Path.Combine(_tempDir, "non-existent.json");

        var result = await MasterDataStore.LoadFromFileAsync(nonExistentPath);

        Assert.That(result, Is.Null, "Should return null for non-existent file");
    }

    [Test]
    public async Task LoadAsync_WithEmptyFile_ShouldReturnNull()
    {
        await File.WriteAllTextAsync(_testFilePath, "");

        var result = await MasterDataStore.LoadFromFileAsync(_testFilePath);

        Assert.That(result, Is.Null, "Should return null for empty file");
    }

    [Test]
    public async Task LoadAsync_WithEmptyJsonObject_ShouldReturnEmptyStore()
    {
        var jsonContent = @"{ ""Cities"": [] }";
        await File.WriteAllTextAsync(_testFilePath, jsonContent);

        var result = await MasterDataStore.LoadFromFileAsync(_testFilePath);

        Assert.That(result, Is.Not.Null, "Should return MasterDataStore instance");
        Assert.That(result!.Cities, Is.Empty, "Cities should be empty");
    }

    [Test]
    public async Task LoadAsync_WithValidJsonFile_ShouldDeserializeData()
    {
        var jsonContent = @"
        {
            ""Cities"": [
                { ""Name"": ""Berlin"", ""Latitude"": 52.52, ""Longitude"": 13.405 },
                { ""Name"": ""Munich"", ""Latitude"": 48.137, ""Longitude"": 11.576 }
            ]
        }";
        await File.WriteAllTextAsync(_testFilePath, jsonContent);

        var result = await MasterDataStore.LoadFromFileAsync(_testFilePath);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Cities, Has.Count.EqualTo(2), "Should load 2 cities");
        Assert.That(result.Cities[0].Name, Is.EqualTo("Berlin"), "First city should be Berlin");
        Assert.That(result.Cities[1].Name, Is.EqualTo("Munich"), "Second city should be Munich");
    }

    [Test]
    public async Task LoadAsync_WithInvalidJson_ShouldReturnNull()
    {
        await File.WriteAllTextAsync(_testFilePath, "{ invalid json content }");

        var result = await MasterDataStore.LoadFromFileAsync(_testFilePath);

        Assert.That(result, Is.Null, "Should return null for invalid JSON");
    }

    [Test]
    public async Task LoadAsync_WithNullPath_ShouldReturnNull()
    {
        var result = await MasterDataStore.LoadFromFileAsync(null!);

        Assert.That(result, Is.Null, "Should return null for null path");
    }

    [Test]
    public async Task LoadAsync_WithEmptyPath_ShouldReturnNull()
    {
        var result = await MasterDataStore.LoadFromFileAsync("");

        Assert.That(result, Is.Null, "Should return null for empty path");
    }

    [Test]
    public async Task LoadAsync_WithMultipleCities_ShouldLoadAll()
    {
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

        var result = await MasterDataStore.LoadFromFileAsync(_testFilePath);

        Assert.That(result!.Cities, Has.Count.EqualTo(5), "Should load all 5 cities");
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
        await File.WriteAllTextAsync(_testFilePath, "   \n  \t  \n  ");

        var result = await MasterDataStore.LoadFromFileAsync(_testFilePath);

        Assert.That(result, Is.Null, "Should return null for whitespace-only file");
    }

    [Test]
    public async Task LoadFromFileAsync_WithViessmannMultiplexSignalsKey_ShouldDeserializeEntries()
    {
        var jsonContent = """
        {
            "schemaVersion": 1,
            "cities": [],
            "locomotives": [],
            "viessmannMultiplexSignals": [
                { "articleNumber": "4046", "displayName": "Ks-Ausfahrsignal (Mehrbereich)", "role": "main" },
                { "articleNumber": "4721", "displayName": "Licht-Blocksignal (Bauart 1969)", "role": "main" }
            ]
        }
        """;
        await File.WriteAllTextAsync(_testFilePath, jsonContent);

        var result = await MasterDataStore.LoadFromFileAsync(_testFilePath);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.MultiplexSignals, Has.Count.EqualTo(2));
        Assert.That(result.MultiplexSignals[0].ArticleNumber, Is.EqualTo("4046"));
        Assert.That(result.MultiplexSignals[1].ArticleNumber, Is.EqualTo("4721"));
    }

    [Test]
    public async Task SaveAsync_ThenLoadFromFileAsync_RoundtripsData()
    {
        var store = new MasterDataStore();
        store.Cities.Add(new City { Name = "Teststadt" });
        store.Locomotives.Add(new LocomotiveCategory { Category = "Test", Series = [] });

        await store.SaveAsync(_testFilePath);
        var loaded = await MasterDataStore.LoadFromFileAsync(_testFilePath);

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Cities, Has.Count.EqualTo(1));
        Assert.That(loaded.Cities[0].Name, Is.EqualTo("Teststadt"));
        Assert.That(loaded.Locomotives, Has.Count.EqualTo(1));
        Assert.That(loaded.Locomotives[0].Category, Is.EqualTo("Test"));
        Assert.That(loaded.SchemaVersion, Is.EqualTo(MasterDataStore.CurrentSchemaVersion));
    }
}