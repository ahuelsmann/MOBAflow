// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Domain;

using global::Moba.Domain;

using System.Text.Json;

internal sealed class LocomotiveLifecycleSerializationTests
{
    [Test]
    public void LocomotiveJson_RemainsReadableWithoutDecoderData()
    {
        const string json =
            """
            {
              "Id": "11111111-1111-1111-1111-111111111111",
              "Name": "Minimal locomotive",
              "DigitalAddress": 3
            }
            """;

        var locomotive = JsonSerializer.Deserialize<Locomotive>(json);

        Assert.Multiple(() =>
        {
            Assert.That(locomotive, Is.Not.Null);
            Assert.That(locomotive!.Name, Is.EqualTo("Minimal locomotive"));
            Assert.That(locomotive.Decoder, Is.Null);
        });
    }

    [Test]
    public void DecoderData_RoundTripsWithoutLosingCvValues()
    {
        var snapshotId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var locomotive = new Locomotive
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "BR 218",
            DigitalAddress = 18,
            Decoder = new LocomotiveDecoderProfile
            {
                Manufacturer = "ESU",
                Model = "LokSound",
                Protocol = DecoderProtocol.Dcc,
                CvSnapshots =
                [
                    new DecoderCvSnapshot
                    {
                        Id = snapshotId,
                        Name = "Factory baseline",
                        CapturedAt = new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero),
                        Values =
                        [
                            new DecoderCvValue { Number = 1, Value = 18 },
                            new DecoderCvValue { Number = 29, Value = 38 }
                        ]
                    }
                ]
            }
        };

        var json = JsonSerializer.Serialize(locomotive);
        var restored = JsonSerializer.Deserialize<Locomotive>(json);

        Assert.Multiple(() =>
        {
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.Decoder!.Protocol, Is.EqualTo(DecoderProtocol.Dcc));
            Assert.That(restored.Decoder.CvSnapshots.Single().Id, Is.EqualTo(snapshotId));
            Assert.That(
                restored.Decoder.CvSnapshots.Single().Values.Select(value => (value.Number, value.Value)),
                Is.EqualTo(new[] { (1, 18), (29, 38) }));
        });
    }

    [Test]
    public void EmptyLifecycleCollections_AreInitializedAfterDeserialization()
    {
        const string json =
            """
            {
              "Manufacturer": "ESU",
              "Protocol": "Dcc"
            }
            """;

        var decoder = JsonSerializer.Deserialize<LocomotiveDecoderProfile>(json);

        Assert.That(decoder!.CvSnapshots, Is.Empty);
    }
}
