// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Data;
using Moba.Domain;

/// <summary>
/// Additional tests for <see cref="MasterDataStore"/> instance operations not covered by file-load tests.
/// </summary>
[TestFixture]
internal sealed class MasterDataStoreUpdateTests
{
    [Test]
    public void UpdateFrom_ReplacesAllCollections()
    {
        var target = new MasterDataStore();
        target.Cities.Add(new City { Name = "Old" });
        target.Locomotives.Add(new LocomotiveCategory { Category = "OldCat", Series = [] });
        target.MultiplexSignals.Add(new MultiplexSignalEntry { ArticleNumber = "1000" });

        var source = new MasterDataStore
        {
            SchemaVersion = 2,
            Cities = [new City { Name = "New" }],
            Locomotives = [new LocomotiveCategory { Category = "NewCat", Series = [new LocomotiveSeries { Name = "BR 110" }] }],
            MultiplexSignals = [new MultiplexSignalEntry { ArticleNumber = "4046", DisplayName = "Signal" }]
        };

        target.UpdateFrom(source);

        Assert.Multiple(() =>
        {
            Assert.That(target.SchemaVersion, Is.EqualTo(2));
            Assert.That(target.Cities, Has.Count.EqualTo(1));
            Assert.That(target.Cities[0].Name, Is.EqualTo("New"));
            Assert.That(target.Locomotives[0].Category, Is.EqualTo("NewCat"));
            Assert.That(target.Locomotives[0].Series[0].Name, Is.EqualTo("BR 110"));
            Assert.That(target.MultiplexSignals[0].ArticleNumber, Is.EqualTo("4046"));
        });
    }

    [Test]
    public void FlattenLocomotiveSeries_OrdersByName()
    {
        var categories = new List<LocomotiveCategory>
        {
            new()
            {
                Category = "A",
                Series =
                [
                    new LocomotiveSeries { Name = "BR 218" },
                    new LocomotiveSeries { Name = "BR 110" }
                ]
            },
            new()
            {
                Category = "B",
                Series = [new LocomotiveSeries { Name = "BR 103" }]
            }
        };

        var flat = MasterDataStore.FlattenLocomotiveSeries(categories);

        Assert.That(flat.Select(s => s.Name), Is.EqualTo(new[] { "BR 103", "BR 110", "BR 218" }));
    }

    [Test]
    public async Task LoadAsync_InstanceMethod_ClearsDataWhenFileMissing()
    {
        var store = new MasterDataStore();
        store.Cities.Add(new City { Name = "Existing" });
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-master-{Guid.NewGuid():N}.json");

        await store.LoadAsync(missingPath);

        Assert.That(store.Cities, Is.Empty);
    }

    [Test]
    public async Task LoadLocomotivesAsync_WithLegacyLibraryFile_ReturnsCategories()
    {
        var path = Path.Combine(Path.GetTempPath(), $"locos-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path,
            """
            {
              "locomotives": [
                {
                  "Category": "Elektro",
                  "Series": [ { "Name": "BR 110", "Vmax": 160 } ]
                }
              ]
            }
            """);

        try
        {
            var categories = await MasterDataStore.LoadLocomotivesAsync(path);

            Assert.Multiple(() =>
            {
                Assert.That(categories, Has.Count.EqualTo(1));
                Assert.That(categories[0].Category, Is.EqualTo("Elektro"));
                Assert.That(categories[0].Series[0].Name, Is.EqualTo("BR 110"));
            });
        }
        finally
        {
            File.Delete(path);
        }
    }
}
