// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Service;
using global::Moba.Domain;

using System.Text.Json;

internal sealed class LocomotiveLibraryServiceTests
{
    private readonly LocomotiveLibraryService _service = new();

    [Test]
    public void BuildLibrary_ReturnsStablePresentationNeutralEntries()
    {
        var laterId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var earlierId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var project = new Project
        {
            Locomotives =
            [
                new Locomotive { Id = laterId, Name = "Zulu", DigitalAddress = 9 },
                new Locomotive { Id = earlierId, Name = "alpha", DigitalAddress = 3 }
            ]
        };

        var entries = _service.BuildLibrary(project);

        Assert.Multiple(() =>
        {
            Assert.That(entries.Select(entry => entry.Name), Is.EqualTo(new[] { "alpha", "Zulu" }));
            Assert.That(entries.Select(entry => entry.LocomotiveId), Is.EqualTo(new[] { earlierId, laterId }));
        });
    }

    [Test]
    public void BuildPassport_HandlesMissingOptionalData()
    {
        var locomotive = new Locomotive
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Minimal"
        };

        var passport = _service.BuildPassport(locomotive);

        Assert.Multiple(() =>
        {
            Assert.That(passport.LocomotiveId, Is.EqualTo(locomotive.Id));
            Assert.That(passport.Decoder, Is.Null);
            Assert.That(passport.LatestMaintenance, Is.Null);
        });
    }

    [Test]
    public void BuildPassport_ProjectsDecoderAndLatestMaintenance()
    {
        var locomotive = new Locomotive
        {
            Name = "BR 218",
            Decoder = new LocomotiveDecoderProfile
            {
                Manufacturer = "ESU",
                Model = "LokSound",
                FirmwareVersion = "1.2",
                Protocol = DecoderProtocol.Dcc
            },
            Maintenance = new LocomotiveMaintenanceData
            {
                Entries =
                [
                    new LocomotiveMaintenanceEntry
                    {
                        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        PerformedAt = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                        Category = MaintenanceCategory.Cleaning,
                        Description = "Old"
                    },
                    new LocomotiveMaintenanceEntry
                    {
                        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        PerformedAt = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
                        Category = MaintenanceCategory.Lubrication,
                        Description = "Latest"
                    }
                ]
            }
        };

        var passport = _service.BuildPassport(locomotive);

        Assert.Multiple(() =>
        {
            Assert.That(passport.Decoder!.Protocol, Is.EqualTo(DecoderProtocol.Dcc));
            Assert.That(passport.LatestMaintenance!.Description, Is.EqualTo("Latest"));
        });
    }

    [Test]
    public void Passport_DoesNotExposePhotoPathsOrNavigationUrls()
    {
        var locomotive = new Locomotive
        {
            Name = "Private path check",
            PhotoPath = @"C:\Users\owner\private.jpg"
        };

        var json = JsonSerializer.Serialize(_service.BuildPassport(locomotive));

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Not.Contain("private.jpg"));
            Assert.That(json, Does.Not.Contain("http"));
            Assert.That(json.ToLowerInvariant(), Does.Not.Contain("qr"));
        });
    }
}
