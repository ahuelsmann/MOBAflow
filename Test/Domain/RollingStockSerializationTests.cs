// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using System.Text.Json;
using global::Moba.Domain;

internal sealed class RollingStockSerializationTests
{
    [Test]
    public void SolutionRoundTrip_PreservesInventoryAndDiscardsRemovedMaintenance()
    {
        // Unknown properties have no retained model representation; no migration is involved.
        const string json = """
            {
              "schemaVersion": 4,
              "name": "Inventory",
              "projects": [{
                "name": "Layout",
                "locomotives": [{
                  "name": "BR 218", "digitalAddress": 18, "photoPath": "locomotives/photo.jpg",
                  "maintenance": {"plans": [{"name": "Removed calendar task", "intervalDays": 365}], "entries": []},
                  "decoder": {"protocol": "Dcc", "cvSnapshots": [{"name": "Backup", "values": [{"number": 1, "value": 18}]}]}
                }],
                "passengerWagons": [{"name": "Coach", "articleNumber": "P1", "maintenance": {"plans": [], "entries": []}}],
                "goodsWagons": [{"name": "Boxcar", "articleNumber": "G1", "maintenance": {"plans": [], "entries": []}}]
              }]
            }
            """;

        var solution = JsonSerializer.Deserialize<Solution>(json, JsonOptions.Default)!;
        var saved = JsonSerializer.Serialize(solution, JsonOptions.Default);
        var restored = JsonSerializer.Deserialize<Solution>(saved, JsonOptions.Default)!.Projects.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(solution.SchemaVersion, Is.EqualTo(Solution.CurrentSchemaVersion));
            Assert.That(saved, Does.Not.Contain("maintenance"));
            Assert.That(saved, Does.Not.Contain("Removed calendar task"));
            Assert.That(restored.Locomotives.Single().DigitalAddress, Is.EqualTo(18));
            Assert.That(restored.Locomotives.Single().PhotoPath, Is.EqualTo("locomotives/photo.jpg"));
            Assert.That(restored.Locomotives.Single().Decoder!.CvSnapshots.Single().Values.Single().Value, Is.EqualTo(18));
            Assert.That(restored.PassengerWagons.Single().ArticleNumber, Is.EqualTo("P1"));
            Assert.That(restored.GoodsWagons.Single().ArticleNumber, Is.EqualTo("G1"));
        }
    }
}
